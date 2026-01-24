import os
import sys
import pandas as pd
import numpy as np
import re
import spacy
import nltk
from nltk.corpus import stopwords
import hashlib

print("pandas:", pd.__version__)
print("numpy:", np.__version__)
print("spacy:", spacy.__version__)
print("nltk:", nltk.__version__)

nlp = spacy.load("en_core_web_sm", disable=["parser", "tagger", "lemmatizer"])
print("spaCy model loaded:", nlp.pipe_names)
print("Python version:", sys.version)
if len(sys.argv) < 3:
    raise SystemExit("Usage: python processor.py <input_csv> <output_csv>")

input_csv = sys.argv[1]
output_csv = sys.argv[2]



df = pd.read_csv(input_csv)
INSTRUCTOR_LIKERT_COLS = [
    "My instructor is knowledgeable in the subject area they are teaching: (I Trust them)",
    "My instructor cultivates a quality educational learning experience: (I Learn from them)",
    "Interaction with my instructor is positive/beneficial: (I Feel Valued by them)",
    "My instructor provides me with opportunities to be challenged: (I Grow from them)",
    "Whether verbal or written  my instructor gives timely  constructive feedback: (I Hear from them)"
]

COURSE_LIKERT_COLS = [
    "The curriculum followed the learning outcomes outlined in the syllabus",
    "The course pacing was appropriate for learning new material",
    "The assignments  exercises  labs  exams  and projects appropriately assessed my learning",
    "The course complexity provided for a reasonably challenging and intellectually stimulating student experience",
    "I learned relevant material in this course that enhances my skillset in at least one of the following areas (Programming  Tech  Problem Solving  Communication  Future Career)"
]

# Display basic information about the dataset
# print(df.head())

# Run once; safe to leave but it will check/download each time.
# nltk.download("stopwords", quiet=True)

EMAIL_RE = re.compile(r"\b[\w\.-]+@[\w\.-]+\.\w+\b")
PHONE_RE = re.compile(r"(\+?\d{1,2}\s*)?(\(?\d{3}\)?[\s.-]?)\d{3}[\s.-]?\d{4}")
URL_RE   = re.compile(r"https?://\S+|www\.\S+")

STOPWORDS = set(stopwords.words("english"))

# IMPORTANT for sentiment: keep negations
for w in ["no", "nor", "not", "never", "n't", "wasn't", "don't", "doesn't", "didn't", "won't", "wouldn't", "can't", "couldn't", "shouldn't"]:
    STOPWORDS.discard(w)
    
def question_key(header: str) -> str:
    h = hashlib.sha1(header.encode("utf-8")).hexdigest()[:10]
    return f"q_{h}"  # like q_8f3a12c9a1

def anonymize_text(text: str) -> str:
    text = EMAIL_RE.sub("[EMAIL]", text)
    text = PHONE_RE.sub("[PHONE]", text)
    text = URL_RE.sub("[URL]", text)

    doc = nlp(text)
    redacted = text

    for ent in sorted(doc.ents, key=lambda e: e.start_char, reverse=True):
        if ent.label_ in {"PERSON", "ORG", "GPE", "LOC"}:
            redacted = redacted[:ent.start_char] + f"[{ent.label_}]" + redacted[ent.end_char:]

    return redacted

def clean_text(text: str) -> str:
    text = text.lower()
    # keep placeholders like [email] by allowing brackets
    text = re.sub(r"[^\w\s\[\]]", " ", text)
    text = re.sub(r"\s+", " ", text).strip()

    tokens = [
        t for t in text.split()
        if t not in STOPWORDS and len(t) > 2
    ]
    return " ".join(tokens)

def preprocess(val) -> str:
    # Handles NaN and non-strings safely
    if not isinstance(val, str):
        return ""
    if not val.strip():
        return ""
    text = anonymize_text(val)
    return clean_text(text)

def safe_float(x):
    try:
        if x is None or (isinstance(x, float) and np.isnan(x)):
            return None
        v = float(x)
        if v == -1:
            return None
        return v
    except:
        return None

def avg_likert(row, cols):
    vals = []
    for c in cols:
        if c in row:
            v = safe_float(row[c])
            if v is not None:
                vals.append(v)
    return float(np.mean(vals)) if vals else None

def label_from_avg(avg):
    if avg is None:
        return None
    if avg >= 4.0:
        return "Positive"
    if avg >= 3.0:
        return "Neutral"
    return "Negative"

# -------- DETECT FREE-TEXT COLUMNS --------
text_cols = [
    c for c in df.columns
    if any(k in c.lower() for k in ["comment", "comments", "what", "other", "appreciate", "improve"])
]

rows = []
likert_rows = []

for _, row in df.iterrows():
    resp_fac = str(row.get("resp_fac", "")).strip().lower()
    is_course_overall = (resp_fac == "(overall)")

    # Metadata
    instructor_name = str(row.get("crs_dir", "") or "").strip()
    course_number = str(row.get("crsnum", "") or "").strip()
    course_name = str(row.get("crsname", "") or "").strip()

    # Compute label sources (Likert averages)
    inst_avg   = avg_likert(row, INSTRUCTOR_LIKERT_COLS) if not is_course_overall else None
    course_avg = avg_likert(row, COURSE_LIKERT_COLS) if is_course_overall else None

    inst_label   = label_from_avg(inst_avg)
    course_label = label_from_avg(course_avg)

    for col in text_cols:
        raw_text = row.get(col, "")
        cleaned = preprocess(raw_text)

        if not cleaned or len(cleaned.split()) < 3:
            continue

        col_l = col.lower()

        # Determine what this comment is ABOUT based on the question header
        is_instructor_comment = any(k in col_l for k in ["instructor", "professor", "teacher", "faculty"])
        target = "Instructor" if is_instructor_comment else "Course"

        # Optional sanity rule: overall rows should only generate Course comments
        if is_course_overall and target == "Instructor":
            continue

        # Pick label to match the target
        label = course_label if target == "Course" else inst_label
        if label is None:
            continue

        qkey = question_key(col)

        rows.append({
            "TargetType": target,
            "InstructorName": instructor_name if target == "Instructor" else "",
            "CourseNumber": course_number,
            "CourseName": course_name,
            "QuestionKey": qkey,
            "QuestionHeader": col,
            "RawText": raw_text if isinstance(raw_text, str) else "",
            "TextClean": cleaned,
            "Label": label
        })
        # For Likert summary
        if is_course_overall:
            vals = [safe_float(row[c]) for c in COURSE_LIKERT_COLS]
            dims = [v for v in vals if v is not None]

            if dims:
                avg = float(np.mean(dims))
                label = label_from_avg(avg)

                likert_rows.append({
                    "TargetType": "Course",
                    "InstructorName": "",
                    "CourseNumber": course_number,
                    "CourseName": course_name,
                    "LikertAvg": avg,
                    "LikertCountUsed": len(dims),
                    "LabelDerived": label,
                    "Dim1Avg": safe_float(row[COURSE_LIKERT_COLS[0]]),
                    "Dim2Avg": safe_float(row[COURSE_LIKERT_COLS[1]]),
                    "Dim3Avg": safe_float(row[COURSE_LIKERT_COLS[2]]),
                    "Dim4Avg": safe_float(row[COURSE_LIKERT_COLS[3]]),
                    "Dim5Avg": safe_float(row[COURSE_LIKERT_COLS[4]])
                })
        else:
            vals = [safe_float(row[c]) for c in INSTRUCTOR_LIKERT_COLS]
            dims = [v for v in vals if v is not None]

            if dims:
                avg = float(np.mean(dims))
                label = label_from_avg(avg)

                likert_rows.append({
                    "TargetType": "Instructor",
                    "InstructorName": instructor_name,
                    "CourseNumber": course_number,
                    "CourseName": course_name,
                    "LikertAvg": avg,
                    "LikertCountUsed": len(dims),
                    "LabelDerived": label,
                    "Dim1Avg": safe_float(row[INSTRUCTOR_LIKERT_COLS[0]]),
                    "Dim2Avg": safe_float(row[INSTRUCTOR_LIKERT_COLS[1]]),
                    "Dim3Avg": safe_float(row[INSTRUCTOR_LIKERT_COLS[2]]),
                    "Dim4Avg": safe_float(row[INSTRUCTOR_LIKERT_COLS[3]]),
                    "Dim5Avg": safe_float(row[INSTRUCTOR_LIKERT_COLS[4]])
                })
        
        

out_df = pd.DataFrame(rows)
out_df.to_csv(output_csv, index=False)
out_dir = os.path.dirname(os.path.abspath(output_csv))
pd.DataFrame(likert_rows).to_csv(os.path.join(out_dir, "likert_summary.csv"), index=False)

qmap = out_df[["QuestionKey", "QuestionHeader"]].drop_duplicates()
qmap.to_csv(os.path.join(out_dir, "question_map.csv"), index=False)

print(f"Exported {len(out_df)} cleaned text rows")
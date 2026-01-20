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

# Display basic information about the dataset
# print(df.head())

# Run once; safe to leave but it will check/download each time.
# nltk.download("stopwords", quiet=True)

EMAIL_RE = re.compile(r"\b[\w\.-]+@[\w\.-]+\.\w+\b")
PHONE_RE = re.compile(r"(\+?\d{1,2}\s*)?(\(?\d{3}\)?[\s.-]?)\d{3}[\s.-]?\d{4}")
URL_RE   = re.compile(r"https?://\S+|www\.\S+")

STOPWORDS = set(stopwords.words("english"))

# IMPORTANT for sentiment: keep negations
for w in ["no", "nor", "not", "never", "n't"]:
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

# -------- DETECT FREE-TEXT COLUMNS --------
text_cols = [
    c for c in df.columns
    if any(k in c.lower() for k in ["comment", "comments", "what", "other", "appreciate", "improve"])
]

rows = []

for _, row in df.iterrows():
    instructor_name = str(row.get("crs_dir", "") or "").strip()
    course_number = str(row.get("crsnum", "") or "").strip()
    course_name = str(row.get("crsname", "") or "").strip()

    for col in text_cols:
        raw_text = row.get(col, "")
        cleaned = preprocess(raw_text)
        
        # Skip very short cleaned strings
        if len(cleaned.split()) < 3:
            continue

        col_l = col.lower()
        target = "Instructor" if any(k in col_l for k in ["instructor", "professor", "teacher", "faculty"]) else "Course"
        
        qkey = question_key(col)

        rows.append({
            "TargetType": target,
            "InstructorName": instructor_name,
            "CourseNumber": course_number,
            "CourseName": course_name,
            "QuestionKey": qkey,
            "QuestionHeader": col,   # keep ONLY in output file for mapping (optional)
            "RawText": anonymize_text(raw_text) if isinstance(raw_text, str) else "",
            "TextClean": cleaned
        })

out_df = pd.DataFrame(rows)
out_df.to_csv(output_csv, index=False)

qmap = out_df[["QuestionKey", "QuestionHeader"]].drop_duplicates()
qmap.to_csv("question_map.csv", index=False)

print(f"Exported {len(out_df)} cleaned text rows")
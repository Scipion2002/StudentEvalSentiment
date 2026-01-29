import pandas as pd

# Path to your existing processed comments CSV
source = r"C:\Users\alexh\OneDrive - Neumont College of Computer Science\Documents\Masters\PRO590\Capstone Project\StudentEvalSentiment\Python\sentiment_text_clean.csv"

df = pd.read_csv(source)

# Keep only useful text
df["TextClean"] = df["TextClean"].fillna("").astype(str).str.strip()

# Filter out too-short comments (helps clustering a lot)
df = df[df["TextClean"].str.split().str.len() >= 3]

# Split by TargetType
inst = df[df["TargetType"].str.lower() == "instructor"][["TextClean"]]
course = df[df["TargetType"].str.lower() == "course"][["TextClean"]]

# Output files (same folder as source)
out_dir = source.rsplit("\\", 1)[0]
inst_path = out_dir + "\\topic_instructor.csv"
course_path = out_dir + "\\topic_course.csv"

inst.to_csv(inst_path, index=False)
course.to_csv(course_path, index=False)

print("Wrote:", inst_path, "rows:", len(inst))
print("Wrote:", course_path, "rows:", len(course))
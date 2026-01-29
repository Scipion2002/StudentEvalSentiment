import os
import sys
import pandas as pd

def main():
    if len(sys.argv) < 2:
        raise SystemExit(
            "Usage: topic_prep.py <processed_comments_csv> [out_dir]\n"
            "Example: topic_prep.py sentiment_text_clean.csv ."
        )

    processed_csv = sys.argv[1]
    out_dir = sys.argv[2] if len(sys.argv) >= 3 else os.path.dirname(os.path.abspath(processed_csv)) or "."

    os.makedirs(out_dir, exist_ok=True)

    df = pd.read_csv(processed_csv)

    # Validate required columns
    required = {"TargetType", "TextClean"}
    missing = required - set(df.columns)
    if missing:
        raise ValueError(f"Missing required columns in processed CSV: {missing}")

    # Clean + filter
    df["TargetType"] = df["TargetType"].fillna("").astype(str).str.strip().str.lower()
    df["TextClean"] = df["TextClean"].fillna("").astype(str).str.strip()

    # Remove too-short comments (helps clustering a LOT)
    df = df[df["TextClean"].str.split().str.len() >= 3]

    instructor_df = df[df["TargetType"] == "instructor"][["TextClean"]].copy()
    course_df = df[df["TargetType"] == "course"][["TextClean"]].copy()

    instructor_path = os.path.join(out_dir, "topic_instructor.csv")
    course_path = os.path.join(out_dir, "topic_course.csv")

    instructor_df.to_csv(instructor_path, index=False)
    course_df.to_csv(course_path, index=False)

    print(f"Wrote {len(instructor_df)} rows -> {instructor_path}")
    print(f"Wrote {len(course_df)} rows -> {course_path}")

if __name__ == "__main__":
    main()
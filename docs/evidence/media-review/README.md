# Submission media and login trace review

Evidence snapshot: `5845c786e8ed02876152a72889d0a05f235c8044`. Run `python3 docs/evidence/media-review/check.py` from the checkout with FFmpeg and English Tesseract installed. The verifier expects readable screenshot text and at least one readable frame per recording. It exits unsuccessfully for a credential pattern hit or an unexpected HAR field value.

AI ran local OCR on all 13 tracked evidence PNGs and all 75 frames of the two tracked evidence recordings. Both recordings contained video only. The verifier searched recognized text and the listed identity evidence documents for email addresses, JWTs, bearer values and labeled credentials. The three HAR exchanges had empty header and cookie arrays, no request or response bodies, no URL query or fragment, and only redacted or explicitly expected query field values. See [the per-file output](results.txt).

No detector matched. OCR can miss small or obscured text, personal names and unlabeled opaque strings. This result is scoped to the listed files and patterns. The original evidence files were left intact. The output contains paths and counts only.

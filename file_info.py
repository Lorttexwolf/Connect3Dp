import hashlib
import sys
import os

if len(sys.argv) < 2:
    print("Usage: python file_info.py <filepath>")
    sys.exit(1)

filepath = sys.argv[1]

if not os.path.exists(filepath):
    print(f"File not found: {filepath}")
    sys.exit(1)

size = os.path.getsize(filepath)

with open(filepath, "rb") as f:
    md5 = hashlib.md5(f.read()).hexdigest()

print(f"File:       {os.path.basename(filepath)}")
print(f"TotalSize:  {size}")
print(f"S-File-MD5: {md5}")

import os
import zipfile

base_dir = os.path.dirname(os.path.abspath(__file__))
zip_path = os.path.join(base_dir, "src", "Installer", "payload.zip")
staging_dir = os.path.join(base_dir, "staging")

files_to_pack = [
    ("SignalNoAr.exe", os.path.join(staging_dir, "SignalNoAr.exe")),
    ("app.ico", os.path.join(staging_dir, "app.ico"))
]

with zipfile.ZipFile(zip_path, 'w', zipfile.ZIP_DEFLATED, compresslevel=6) as zf:
    for arcname, path in files_to_pack:
        if os.path.exists(path):
            zf.write(path, arcname)

print("Repacked payload.zip successfully:", os.path.getsize(zip_path), "bytes")

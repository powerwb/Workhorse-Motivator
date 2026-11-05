import os
import shutil
import struct
import subprocess
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "src", "anim.bin")

video = sys.argv[1] if len(sys.argv) > 1 else os.path.join(
    os.path.expanduser("~"), "Downloads", "grok-video-c528db10-2ff3-46ae-9ff2-ed5b12c4e81d.mp4")
fps = int(sys.argv[2]) if len(sys.argv) > 2 else 12
width = int(sys.argv[3]) if len(sys.argv) > 3 else 480
height = int(round(width * 9 / 16.0))

if not os.path.exists(video):
    sys.exit("找不到视频：" + video)

tmp = tempfile.mkdtemp(prefix="niuma-frames-")
try:
    cmd = ["ffmpeg", "-y", "-v", "error", "-i", video,
           "-vf", "fps=%d,scale=%d:%d:flags=lanczos" % (fps, width, height),
           "-q:v", "4", os.path.join(tmp, "%04d.jpg")]
    subprocess.check_call(cmd)

    files = sorted(f for f in os.listdir(tmp) if f.endswith(".jpg"))
    if not files:
        sys.exit("没有抽到帧")

    blobs = []
    for f in files:
        with open(os.path.join(tmp, f), "rb") as fh:
            blobs.append(fh.read())

    delay = int(round(1000.0 / fps))
    with open(OUT, "wb") as out:
        out.write(b"NMAF")
        out.write(struct.pack("<iiiii", 1, width, height, delay, len(blobs)))
        for b in blobs:
            out.write(struct.pack("<i", len(b)))
        for b in blobs:
            out.write(b)

    total = sum(len(b) for b in blobs)
    print("帧数 %d，单帧 %dx%d，间隔 %dms，总时长 %.2fs" % (len(blobs), width, height, delay, len(blobs) * delay / 1000.0))
    print("JPEG 合计 %.2f MB → %s (%.2f MB)" % (total / 1048576.0, OUT, os.path.getsize(OUT) / 1048576.0))

                              
    prev = os.path.join(ROOT, "src", "anim-preview.jpg")
    shutil.copyfile(os.path.join(tmp, files[0]), prev)
finally:
    shutil.rmtree(tmp, ignore_errors=True)

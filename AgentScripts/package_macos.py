"""Package the built macOS player with notices, an Applications shortcut, and checksums."""
from pathlib import Path
import hashlib
import shutil
import subprocess

root = Path(__file__).resolve().parent.parent
source = root / 'Builds/macOS/CloudRail.app'
if not source.is_dir():
    raise SystemExit('Build CloudRail.app from the Unity menu first.')
stage = root / 'Builds/ReleaseStaging'
stage.mkdir(parents=True, exist_ok=True)
app = stage / 'CloudRail.app'
subprocess.run(['/usr/bin/ditto', str(source), str(app)], check=True)
licenses = app / 'Contents/Resources/Licenses'
licenses.mkdir(parents=True, exist_ok=True)
for source_file, target_name in [
    (root/'LICENSE', 'CloudRail-MIT.txt'),
    (root/'THIRD_PARTY_NOTICES.md', 'THIRD_PARTY_NOTICES.md'),
    (root/'Assets/CloudRail/Fonts/OFL.txt', 'NotoSansCJK-OFL.txt'),
    (root/'Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt', 'LiberationSans-OFL.txt'),
]:
    shutil.copy2(source_file, licenses/target_name)
for name in ['LICENSE', 'THIRD_PARTY_NOTICES.md']:
    shutil.copy2(root/name, stage/name)
applications = stage/'Applications'
if not applications.exists():
    applications.symlink_to('/Applications', target_is_directory=True)
(stage/'安装说明.txt').write_text('CloudRail v0.1.0 · 云上列车\n\n将 CloudRail.app 拖到 Applications。macOS 12+，Apple Silicon / Intel。\nC 切换机位；右键环绕；滚轮或中键拖动缩放；空格暂停；Tab 切换探索。\n\n这是本地签名、未经 Apple 公证的开源试玩包。首次打开若被系统阻止，请在确认来源后参阅 Apple 说明：\nhttps://support.apple.com/zh-cn/102445\n\n源码：https://github.com/Hao2048/CloudRail\n', encoding='utf-8')
# Adding the accompanying license files changes the bundle seal. Re-sign locally;
# this is ad-hoc signing, not a Developer ID identity or notarization.
subprocess.run(['codesign', '--force', '--sign', '-', '--timestamp=none', str(app)], check=True)
subprocess.run(['codesign', '--verify', '--deep', '--strict', str(app)], check=True)
out = root/'Releases'
out.mkdir(exist_ok=True)
subprocess.run(['hdiutil','create','-volname','CloudRail','-srcfolder',str(stage),'-format','UDZO','-ov','-o',str(out/'CloudRail-v0.1.0-macOS-universal.dmg')],check=True)
archive = out/'CloudRail-v0.1.0-macOS-universal.zip'
if archive.exists():
    archive.unlink()  # Replace this generated output, preserving the local built app.
subprocess.run(['/usr/bin/ditto','-c','-k','--sequesterRsrc','--keepParent',str(app),str(archive)],check=True)
files = sorted(p for p in out.iterdir() if p.suffix in ['.dmg','.zip','.mp4'])
(out/'SHA256SUMS.txt').write_text(''.join(hashlib.sha256(p.read_bytes()).hexdigest()+'  '+p.name+'\n' for p in files))
print('Packaged with full font licenses:', ', '.join(p.name for p in files))

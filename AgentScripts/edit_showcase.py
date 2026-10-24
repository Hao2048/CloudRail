"""Edit the four actual Unity camera takes into the GitHub showcase."""
from pathlib import Path
import subprocess
import sys

project = Path(__file__).resolve().parent.parent
here = Path(sys.argv[1]).resolve() if len(sys.argv)>1 else project/'Documentation/Showcase/BirdsEdition'
audio = project / 'Assets/CloudRail/Audio'
font = '/System/Library/Fonts/Hiragino Sans GB.ttc'
latin = '/System/Library/Fonts/HelveticaNeue.ttc'
filters = '''[0:v]split=4[s0][s1][s2][s3];
[s0]trim=start=0:end=8,setpts=PTS-STARTPTS,setsar=1[v0];
[s1]trim=start=8:end=16,setpts=PTS-STARTPTS,setsar=1[v1];
[s2]trim=start=16:end=24,setpts=PTS-STARTPTS,setsar=1[v2];
[s3]trim=start=24:end=32,setpts=PTS-STARTPTS,setsar=1[v3];
[v0][v1]xfade=transition=fade:duration=0.35:offset=7.65[x1];
[x1][v2]xfade=transition=fade:duration=0.35:offset=15.30[x2];
[x2][v3]xfade=transition=fade:duration=0.35:offset=22.95,
format=yuv420p[base];
[5:v]format=rgba,fade=t=in:st=26:d=0.8:alpha=1[title];
[base][title]overlay=shortest=1,fade=t=out:st=30.35:d=0.6[v];
[1:a]atrim=duration=30.95,asetpts=PTS-STARTPTS,volume=0.85[m];
[2:a]atrim=duration=30.95,asetpts=PTS-STARTPTS,volume=0.14[r];
[3:a]atrim=duration=30.95,asetpts=PTS-STARTPTS,volume=0.20[w];
[4:a]volume=0.18,adelay=450|450[h];
[m][r][w][h]amix=inputs=4:duration=first:normalize=0,alimiter=limit=0.9,afade=t=in:d=0.7,afade=t=out:st=28.8:d=2.15[a]
'''.format(font=font,latin=latin)
(here/'edit.ffscript').write_text(filters)
subprocess.run(['ffmpeg','-y','-hide_banner','-loglevel','warning','-filter_complex_threads','2',
 '-i',str(here/'CloudRail_Cinematic_Silent.mp4'),
 '-i',str(audio/'Cloudline_Theme.wav'),
 '-stream_loop','-1','-i',str(audio/'Rail_Rhythm.wav'),
 '-stream_loop','-1','-i',str(audio/'Cloud_Wind.wav'),
 '-i',str(audio/'Departure_Whistle.wav'),
 '-loop','1','-framerate','30','-i',str(here/'title.png'),
 '-filter_complex',filters,'-map','[v]','-map','[a]',
 '-c:v','libx264','-preset','slow','-crf','19','-pix_fmt','yuv420p',
 '-c:a','aac','-b:a','192k','-t','30.95','-movflags','+faststart',
 str(here/'CloudRail_GitHub_Showcase.mp4')],check=True)
# A clear train close-up as the poster, rather than a generic title card.
subprocess.run(['ffmpeg','-y','-hide_banner','-loglevel','warning','-ss','2.4','-i',str(here/'CloudRail_Cinematic_Silent.mp4'),'-frames:v','1','-q:v','2',str(here/'CloudRail_Cover.jpg')],check=True)
from PIL import Image,ImageDraw,ImageFont
cover=Image.open(here/'CloudRail_Cover.jpg');drawing=ImageDraw.Draw(cover)
drawing.text((104,95),'云上列车',font=ImageFont.truetype(font,68),fill='#24546c')
drawing.text((108,187),'CLOUDRAIL',font=ImageFont.truetype(latin,28),fill='#38768c')
cover.resize((1280,720),Image.Resampling.LANCZOS).save(here/'CloudRail_Cover.jpg',quality=95)
subprocess.run(['ffmpeg','-y','-hide_banner','-loglevel','warning','-ss','0.6','-t','6','-i',str(here/'CloudRail_GitHub_Showcase.mp4'),'-filter_complex','fps=12,scale=640:-1:flags=lanczos,split[a][b];[a]palettegen=stats_mode=diff[p];[b][p]paletteuse=dither=bayer:bayer_scale=3','-loop','0',str(here/'CloudRail_Preview.gif')],check=True)

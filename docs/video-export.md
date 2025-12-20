# 프레임 이미지 동영상 변환

생성된 이미지 프레임을 동영상으로 변환하려면 FFmpeg가 필요합니다.

## 설치

FFmpeg는 공식 사이트에서 설치할 수 있습니다:

- https://ffmpeg.org/

## 변환 명령

프로젝트 루트에서 아래 명령을 실행하면 `output.mp4`가 생성됩니다.

```bash
ffmpeg -framerate 60 -i output/frame_%04d.png -c:v libx264 -pix_fmt yuv420p output.mp4
```

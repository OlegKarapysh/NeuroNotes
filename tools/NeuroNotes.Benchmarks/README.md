# NeuroNotes.Benchmarks (BenchmarkDotNet)

Micro-benchmarks for the local voice pipeline, using the **real production classes**
(`WhisperSpeechRecognizer` + `WhisperProcessorFactory`, `FFmpegAudioConverter`).

Use this for the **CPU / latency / managed-allocation** axis — BenchmarkDotNet gives rigorous timing
statistics (mean, median, stddev, percentiles, outlier handling) and GC/allocation counts.

## ⚠️ It does NOT measure the RAM that matters

`[MemoryDiagnoser]` reports **managed-heap allocations only**. Whisper's real footprint is **native**
(`whisper_state`, ggml buffers) and ffmpeg's is a **separate process** — neither shows up in the
`Allocated` column. For the actual per-transcription RSS (the 1 GB-droplet ceiling) use the
`loadtest --isolate` probe in the WebApi host, or OS tools (`docker stats`, `/usr/bin/time -v`,
`pidstat`). Treat the two as complementary: **this = CPU/latency, that = RAM.**

## Benchmarks

| Benchmark | What it measures |
|-----------|------------------|
| `Whisper transcription (no FFmpeg)` | `ISpeechRecognizer.RecognizeSpeech` on a pre-converted WAV — the headline per-note CPU/latency. |
| `FFmpeg OGG->WAV` | `IAudioConverter.ConvertOggToWav` — managed cost only (native ffmpeg is out-of-process). |

It uses `RunStrategy.Monitoring` (1 warmup + 10 real iterations) because each transcription takes
seconds — BenchmarkDotNet's default high-invocation pilot would be impractical.

## Run

Provide a **real speech** `.ogg`/`.oga` voice note (a tone/silence under-represents Whisper's decode
cost). The model defaults to the copy vendored next to the binary; override with `WHISPER_MODEL`.

```bash
# from the repo root
export SAMPLE_OGG=/abs/path/to/voice.ogg     # PowerShell: $env:SAMPLE_OGG = "C:\path\voice.ogg"
# export WHISPER_MODEL=/abs/path/to/ggml-base.bin   # optional override
# export FFMPEG_PATH=ffmpeg                          # optional; default "ffmpeg" (must be on PATH)

dotnet run -c Release --project tools/NeuroNotes.Benchmarks
```

Results print to the console and are written under `BenchmarkDotNet.Artifacts/`.
Run on the droplet (or a 1 vCPU / 1 GB VM) for numbers that reflect production hardware — the dev-box
figures are optimistic and won't show the single-vCPU serialization.

> Not added to `NeuroNotes.slnx` (it's an ad-hoc tool, and `dotnet test --solution` shouldn't pick it
> up). Build/run it directly via `--project` as above.

using BenchmarkDotNet.Running;
using NeuroNotes.Benchmarks;

// Run: set SAMPLE_OGG to a real voice note, then
//   dotnet run -c Release --project tools/NeuroNotes.Benchmarks
// See README.md in this folder.
BenchmarkRunner.Run<TranscriptionBenchmarks>(config: null, args: args);

using FluentResults;

using NeuroNotes.AudioProcessing.Application;
using NeuroNotes.AudioProcessing.Application.Interfaces;

namespace NeuroNotes.AudioProcessing.UnitTests;

public class VoiceTranscriberTests
{
    [Fact]
    public async Task Transcribe_ReturnsRecognizedText_WhenConversionAndRecognitionSucceed()
    {
        var converter = new FakeAudioConverter(Result.Ok<Stream>(new TrackingStream()));
        var recognizer = new FakeSpeechRecognizer(Result.Ok("hello world"));
        var transcriber = new VoiceTranscriber(converter, recognizer);

        var result = await transcriber.Transcribe(new MemoryStream());

        Assert.True(result.IsSuccess);
        Assert.Equal("hello world", result.Value);
        Assert.True(recognizer.WasCalled);
    }

    [Fact]
    public async Task Transcribe_PropagatesFailure_AndSkipsRecognition_WhenConversionFails()
    {
        var converter = new FakeAudioConverter(Result.Fail<Stream>("conversion blew up"));
        var recognizer = new FakeSpeechRecognizer(Result.Ok("should not be returned"));
        var transcriber = new VoiceTranscriber(converter, recognizer);

        var result = await transcriber.Transcribe(new MemoryStream());

        Assert.True(result.IsFailed);
        Assert.Equal("conversion blew up", result.Errors.First().Message);
        Assert.False(recognizer.WasCalled);
    }

    [Fact]
    public async Task Transcribe_PropagatesFailure_WhenRecognitionFails()
    {
        var wavStream = new TrackingStream();
        var converter = new FakeAudioConverter(Result.Ok<Stream>(wavStream));
        var recognizer = new FakeSpeechRecognizer(Result.Fail<string>("recognition blew up"));
        var transcriber = new VoiceTranscriber(converter, recognizer);

        var result = await transcriber.Transcribe(new MemoryStream());

        Assert.True(result.IsFailed);
        Assert.Equal("recognition blew up", result.Errors.First().Message);
        Assert.True(recognizer.WasCalled);
        Assert.True(wavStream.WasDisposed);
    }

    private sealed class FakeAudioConverter(Result<Stream> result) : IAudioConverter
    {
        public Task<Result<Stream>> ConvertOggToWav(MemoryStream oggData, CancellationToken cancellationToken = default) =>
            Task.FromResult(result);
    }

    private sealed class FakeSpeechRecognizer(Result<string> result) : ISpeechRecognizer
    {
        public bool WasCalled { get; private set; }

        public Task<Result<string>> RecognizeSpeech(Stream speech, CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            return Task.FromResult(result);
        }
    }

    private sealed class TrackingStream : MemoryStream
    {
        public bool WasDisposed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            WasDisposed = true;
            base.Dispose(disposing);
        }
    }
}
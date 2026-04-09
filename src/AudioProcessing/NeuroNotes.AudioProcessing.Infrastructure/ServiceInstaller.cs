using Microsoft.Extensions.DependencyInjection;
using NeuroNotes.AudioProcessing.Application;
using NeuroNotes.AudioProcessing.Application.Interfaces;
using NeuroNotes.AudioProcessing.Infrastructure.AudioConversion;
using NeuroNotes.AudioProcessing.Infrastructure.SpeechRecognition;
using NeuroNotes.AudioProcessing.Public.Interfaces;

namespace NeuroNotes.AudioProcessing.Infrastructure;

public static class ServiceInstaller
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddAudioProcessingModule()
        {
            services.ConfigureAudioConversionOptions().AddAudioConversion();
            services.ConfigureSpeechRecognitionOptions().AddWhisperServices();
            services.AddScoped<IVoiceTranscriber, VoiceTranscriber>();

            return services;
        }
        
        public IServiceCollection ConfigureAudioConversionOptions()
        {
            services.AddOptions<AudioConversionOptions>()
                .BindConfiguration(AudioConversionOptions.SectionName)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            return services;
        }

        public IServiceCollection AddAudioConversion()
        {
            return services.AddScoped<IAudioConverter, FFmpegAudioConverter>();
        }
        
        public IServiceCollection ConfigureSpeechRecognitionOptions()
        {
            services.AddOptions<SpeechRecognitionOptions>()
                .BindConfiguration(SpeechRecognitionOptions.SectionName)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            return services;
        }

        public IServiceCollection AddWhisperServices()
        {
            return services.AddScoped<ISpeechRecognizer, WhisperSpeechRecognizer>()
                .AddSingleton<WhisperProcessorFactory>();
        }
    }
}
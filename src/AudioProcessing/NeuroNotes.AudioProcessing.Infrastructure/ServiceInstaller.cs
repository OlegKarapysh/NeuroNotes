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
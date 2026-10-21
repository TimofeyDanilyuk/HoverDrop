namespace HoverDrop.App.Models
{
    public enum FileActionType
    {
        // Общие
        Delete,
        CopyPath,
        CopyPaths,
        MakeCopy,
        Desktop,
        ToZip,
        ExtractHere,
        ExtractToFolder,
        CollectToFolder,

        // Графика
        ToJpg,
        ToPng,
        ToIco,
        ToPdf,
        Blur,
        Grayscale,

        // Видео / Аудио
        ToMp3Audio,
        ToMp3,
        ToWav,
        ToGif,
        Compress720p,
        Compress64k,
        RemoveAudio
    }
}


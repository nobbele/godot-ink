#if TOOLS

#nullable enable

using Godot;
using Godot.Collections;
using Ink;

namespace GodotInk;

[Tool]
public partial class InkStoryImporter : EditorImportPlugin
{
    private const string OPT_MASTER_FILE = "is_master_file";
    private const string OPT_COMPRESS = "compress";

    public override string _GetImporterName() => "ink";

    public override string _GetVisibleName() => "Ink story";

    public override string[] _GetRecognizedExtensions() => new string[] { "ink" };

    public override string _GetResourceType() => "Resource";

    public override string _GetSaveExtension() => "res";

    public override double _GetPriority() => 1.0;

    public override long _GetPresetCount() => 0;

    public override long _GetImportOrder() => 0;

    public override Array<Dictionary> _GetImportOptions(string path, long presetIndex) => new()
    {
        new() { { "name", OPT_MASTER_FILE }, { "default_value", false } },
        new() { { "name", OPT_COMPRESS }, { "default_value", true } }
    };

    public override bool _GetOptionVisibility(string path, StringName optionName, Dictionary options) => true;

    public override long _Import(string sourceFile, string savePath,
                                 Dictionary options, Array<string> platformVariants, Array<string> genFiles)
    {
        string destFile = $"{savePath}.{_GetSaveExtension()}";

        if (!options[OPT_MASTER_FILE].AsBool())
            return (long)ResourceSaver.Save(new Resource(), destFile);

        return (long)ImportFromInk(sourceFile, destFile, options[OPT_COMPRESS].AsBool());
    }

    private static Error ImportFromInk(string sourceFile, string destFile, bool shouldCompress)
    {
        using FileAccess file = FileAccess.Open(sourceFile, FileAccess.ModeFlags.Read);

        if (file == null)
            return FileAccess.GetOpenError();

        Compiler compiler = new(file.GetAsText(), new Compiler.Options
        {
            sourceFilename = sourceFile,
            errorHandler = InkCompilerErrorHandler,
        });

        try
        {
            string storyContent = compiler.Compile().ToJson();
            InkStory resource = InkStory.Create(storyContent);
            ResourceSaver.SaverFlags flags = shouldCompress ? ResourceSaver.SaverFlags.Compress
                                                            : ResourceSaver.SaverFlags.None;
            return ResourceSaver.Save(resource, destFile, flags);
        }
        catch (InvalidInkException)
        {
            return Error.CompilationFailed;
        }
    }

    private static void InkCompilerErrorHandler(string message, ErrorType errorType)
    {
        switch (errorType)
        {
            case ErrorType.Warning:
                GD.PushWarning(message);
                break;
            case ErrorType.Error:
                GD.PushError(message);
                throw new InvalidInkException();
        }
    }

    private class InvalidInkException : System.Exception
    {
        public InvalidInkException() : base()
        {
        }

        public InvalidInkException(string message) : base(message)
        {
        }
    }
}

#endif
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UndertaleModLib;
using UndertaleModLib.Models;
using static UndertaleModLib.Models.UndertaleSound;
using static UndertaleModLib.UndertaleData;

EnsureDataLoaded();

int maxCount = 1;

int audioID = -1;
int audioGroupID = -1;
int embAudioID = -1;
bool usesAGRP = (Data.AudioGroups.Count > 0);

if (!usesAGRP)
{
    ScriptWarning("This game doesn't use audiogroups.\nImporting to external audiogroups is disabled.");
}

// 自动设置 importFolder 为真实游戏目录（data.win 源目录），避免 data.win 被复制到临时路径后扫描不到。
// GameDirectory 为 UtmtScriptGlobals 传入的用户真实选中目录，若为空则退回到 FilePath 所在目录。
string importFolder = !string.IsNullOrEmpty(GameDirectory) && Directory.Exists(GameDirectory)
    ? GameDirectory
    : Path.GetDirectoryName(FilePath);
if (string.IsNullOrWhiteSpace(importFolder) || !Directory.Exists(importFolder))
{
    throw new ScriptException("The import folder was not set.");
}

string[] dirFiles = Directory.Exists(importFolder)
    ? Directory.GetFiles(importFolder)
    : Array.Empty<string>();
string folderName = new DirectoryInfo(importFolder).Name;

bool replaceSoundPropertiesCheck = true;

// 把音乐内置进 data.win：强制 embed 且默认 decode on load
bool GeneralSound_embedSound = true;
bool GeneralSound_decodeLoad = true;
bool GeneralSound_needAGRP = false;
bool manuallySpecifyEverySound = false;

if (GeneralSound_embedSound && usesAGRP)
{
    GeneralSound_needAGRP = false;
}

int validCount = dirFiles.Count(file =>
{
    string f = Path.GetFileName(file);
    return f.EndsWith(".ogg", StringComparison.InvariantCultureIgnoreCase)
           || f.EndsWith(".wav", StringComparison.InvariantCultureIgnoreCase);
});
maxCount = validCount;

if (maxCount <= 0)
{
    ScriptWarning("在 data.win 所在目录未检测到 .ogg/.wav 音乐文件，音乐导入步骤已跳过。");
    ScriptMessage("Sounds skipped: no .ogg/.wav files in the game folder.");
    return;
}

SetProgressBar(null, "Importing sounds", 0, maxCount);
StartProgressBarUpdater();

await Task.Run(() =>
{
    foreach (string file in dirFiles)
    {
        string filename = Path.GetFileName(file);
        if (!(filename.EndsWith(".ogg", StringComparison.InvariantCultureIgnoreCase) || filename.EndsWith(".wav", StringComparison.InvariantCultureIgnoreCase)))
        {
            // Ignore invalid file extensions.
            continue;
        }

        IncProgressLocal();

        string soundName = Path.GetFileNameWithoutExtension(file);
        bool isOGG = Path.GetExtension(filename).Equals(".ogg", StringComparison.OrdinalIgnoreCase);
        bool embedSound;
        bool decodeLoad;
        if (isOGG && !manuallySpecifyEverySound)
        {
            embedSound = GeneralSound_embedSound;
            decodeLoad = GeneralSound_decodeLoad;
        }
        else
        {
            // WAV 必须内置
            embedSound = true;
            decodeLoad = false;
        }
        string audioGroupName = string.Empty;
        string loopFolderName = new DirectoryInfo(importFolder).Name;
        bool needAGRP = false;

        // 查找同名 sound
        UndertaleSound existingSound = null;
        for (var i = 0; i < Data.Sounds.Count; i++)
        {
            if (Data.Sounds[i]?.Name?.Content == soundName)
            {
                existingSound = Data.Sounds[i];
                break;
            }
        }

        // 音频组逻辑：不使用外部 audiogroup（保持内置）
        if (embedSound && usesAGRP && existingSound is null)
        {
            needAGRP = GeneralSound_needAGRP;
        }
        if (needAGRP && usesAGRP && embedSound)
        {
            audioGroupName = loopFolderName;

            if (audioGroupID == -1)
            {
                // Find the audio group we need.
                for (int i = 0; i < Data.AudioGroups.Count; i++)
                {
                    if (Data.AudioGroups[i]?.Name?.Content == audioGroupName)
                    {
                        audioGroupID = i;
                        break;
                    }
                }
                if (audioGroupID == -1)
                {
                    // Still -1? Create a new one...
                    File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(FilePath), $"audiogroup{Data.AudioGroups.Count}.dat"), Convert.FromBase64String("Rk9STQwAAABBVURPBAAAAAAAAAA="));
                    UndertaleAudioGroup newAudioGroup = new()
                    {
                        Name = Data.Strings.MakeString(audioGroupName),
                    };
                    MainThreadAction(() =>
                    {
                        Data.AudioGroups.Add(newAudioGroup);
                    });
                }
            }
        }

        // If this is an existing sound, use its audio group ID.
        if (existingSound is not null)
        {
            audioGroupID = existingSound.GroupID;
        }

        // If the audiogroup ID is for the builtin audiogroup ID, it's embedded in the main data file and doesn't need to be loaded.
        if (audioGroupID == Data.GetBuiltinSoundGroupID())
        {
            needAGRP = false;
        }

        // Create embedded audio entry if required.
        UndertaleEmbeddedAudio soundData = null;
        if ((embedSound && !needAGRP) || needAGRP)
        {
            soundData = new UndertaleEmbeddedAudio() { Data = File.ReadAllBytes(file) };
            // 不在后台线程直接操作 Data.EmbeddedAudio，放到 MainThreadAction 中
        }

        // Update external audio group file if required.
        if (needAGRP)
        {
            UndertaleData audioGroupDat;
            string relativeAudioGroupPath;
            if (audioGroupID < Data.AudioGroups.Count && Data.AudioGroups[audioGroupID] is UndertaleAudioGroup { Path.Content: string customRelativePath })
            {
                relativeAudioGroupPath = customRelativePath;
            }
            else
            {
                relativeAudioGroupPath = $"audiogroup{audioGroupID}.dat";
            }
            string audioGroupPath = Path.Combine(Path.GetDirectoryName(FilePath), relativeAudioGroupPath);
            using (FileStream audioGroupReadStream = new(audioGroupPath, FileMode.Open, FileAccess.Read))
            {
                audioGroupDat = UndertaleIO.Read(audioGroupReadStream);
            }

            audioGroupDat.EmbeddedAudio.Add(soundData);
            if (existingSound is not null && existingSound.AudioFile is not null)
            {
                audioGroupDat.EmbeddedAudio.Remove(existingSound.AudioFile);
            }
            audioID = audioGroupDat.EmbeddedAudio.Count - 1;

            using FileStream audioGroupWriteStream = new(audioGroupPath, FileMode.Create);
            UndertaleIO.Write(audioGroupWriteStream, audioGroupDat);
        }

        // Determine sound flags.
        UndertaleSound.AudioEntryFlags flags = UndertaleSound.AudioEntryFlags.Regular;
        if (isOGG && embedSound && decodeLoad)
        {
            flags = UndertaleSound.AudioEntryFlags.IsEmbedded | UndertaleSound.AudioEntryFlags.IsCompressed | UndertaleSound.AudioEntryFlags.Regular;
        }
        else if (isOGG && embedSound && !decodeLoad)
        {
            flags = UndertaleSound.AudioEntryFlags.IsCompressed | UndertaleSound.AudioEntryFlags.Regular;
        }
        else if (!isOGG)
        {
            flags = UndertaleSound.AudioEntryFlags.IsEmbedded | UndertaleSound.AudioEntryFlags.Regular;
        }
        else if (isOGG && !embedSound)
        {
            flags = UndertaleSound.AudioEntryFlags.Regular;
            audioID = -1;
        }

        // Determine final embedded audio reference (or null).
        UndertaleEmbeddedAudio finalAudioReference = null;
        if (!embedSound)
        {
            finalAudioReference = null;
        }
        if (embedSound && !needAGRP)
        {
            // MainThreadAction 里再真正加入并更新引用
            finalAudioReference = soundData;
        }
        if (embedSound && needAGRP)
        {
            finalAudioReference = null;
        }

        // Determine final audio group reference (or null).
        UndertaleAudioGroup finalGroupReference = null;
        if (!usesAGRP)
        {
            finalGroupReference = null;
        }
        else
        {
            finalGroupReference = needAGRP ? Data.AudioGroups[audioGroupID] : Data.AudioGroups[Data.GetBuiltinSoundGroupID()];
        }

        // 所有 Data 写入操作放在 MainThreadAction 中，避免多线程修改 UndertaleData
        MainThreadAction(() =>
        {
            int localEmbAudioID = embAudioID;
            UndertaleEmbeddedAudio localFinalAudioRef = finalAudioReference;

            if (embedSound && !needAGRP)
            {
                if (existingSound is not null && existingSound.AudioFile is not null)
                {
                    Data.EmbeddedAudio.Remove(existingSound.AudioFile);
                }
                Data.EmbeddedAudio.Add(soundData);
                localEmbAudioID = Data.EmbeddedAudio.Count - 1;
                localFinalAudioRef = Data.EmbeddedAudio[localEmbAudioID];
            }
            else if (embedSound && needAGRP)
            {
                if (existingSound is not null && existingSound.AudioFile is not null)
                {
                    Data.EmbeddedAudio.Remove(existingSound.AudioFile);
                }
            }

            if (existingSound is null)
            {
                UndertaleSound newSound = new()
                {
                    Name = Data.Strings.MakeString(soundName),
                    Flags = flags,
                    Type = isOGG ? Data.Strings.MakeString(".ogg") : Data.Strings.MakeString(".wav"),
                    File = Data.Strings.MakeString(filename),
                    Effects = 0,
                    Volume = 1.0f,
                    Pitch = 1.0f,
                    AudioID = audioID,
                    AudioFile = localFinalAudioRef,
                    AudioGroup = finalGroupReference,
                    GroupID = needAGRP ? audioGroupID : Data.GetBuiltinSoundGroupID()
                };
                Data.Sounds.Add(newSound);
            }
            else if (replaceSoundPropertiesCheck)
            {
                existingSound.Flags = flags;
                existingSound.Type = isOGG ? Data.Strings.MakeString(".ogg") : Data.Strings.MakeString(".wav");
                existingSound.File = Data.Strings.MakeString(filename);
                existingSound.Effects = 0;
                existingSound.Volume = 1.0f;
                existingSound.Pitch = 1.0f;
                existingSound.AudioID = audioID;
                existingSound.AudioFile = localFinalAudioRef;
                existingSound.AudioGroup = finalGroupReference;
                existingSound.GroupID = needAGRP ? audioGroupID : Data.GetBuiltinSoundGroupID();
            }
            else
            {
                existingSound.AudioFile = localFinalAudioRef;
                existingSound.AudioID = audioID;
            }
        });
    }
});

await StopProgressBarUpdater();
ScriptMessage("Sounds added successfully!");


void IncProgressLocal()
{
    if (GetProgress() < maxCount)
    {
        IncrementProgress();
    }
}

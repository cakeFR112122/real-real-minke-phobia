using System;
using System.IO;
using System.Threading.Tasks;
using Liv.NativeGalleryBridge;
using UnityEngine;

namespace Liv.Lck.Utilities
{
    public static class FileUtility
    {
        public static bool IsFileLocked(string filePath)
        {
            FileStream stream = null;

            try
            {
                stream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                return true;
            }
            finally
            {
                stream?.Close();
            }
            return false;
        }

        public static async Task CopyToGallery(string sourceRecordingFilePath, string albumName, Action<bool, string> callback)
        {
            if (File.Exists(sourceRecordingFilePath))
            {
                try
                {
                    var fileName = Path.GetFileName(sourceRecordingFilePath);

                    if (Application.platform == RuntimePlatform.Android)
                    {
                        var permission = await NativeGallery.SaveVideoToGallery(sourceRecordingFilePath, albumName,
                            fileName, new NativeGallery.MediaSaveCallback(callback));
                    }
                    else
                    {
                        var videosFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
                        var albumPath = Path.Combine(videosFolderPath, albumName);

                        if (!Directory.Exists(albumPath))
                        {
                            Directory.CreateDirectory(albumPath);
                        }

                        var destinationFilePath = Path.Combine(albumPath, fileName);
                
                        // Use Task.Run to perform file copying asynchronously
                        await Task.Run(() => File.Copy(sourceRecordingFilePath, destinationFilePath, overwrite: true));
                
                        // Invoke the callback on the main thread if necessary
                        callback.Invoke(true, destinationFilePath);
                    }
                }
                catch (Exception ex)
                {
                    callback.Invoke(false, sourceRecordingFilePath);
                    LckLog.LogError($"LCK Error reading file: {ex.Message}");
                }
            }
            else
            {
                callback.Invoke(false, sourceRecordingFilePath);
                LckLog.LogError($"LCK Source recording does not exist: {sourceRecordingFilePath}");
            }
        }
    }
}

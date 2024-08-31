using R2API;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace BanditSaoriVoiceoverPlugin.Modules
{
    public static class SoundBanks
    {
        private static bool initialized = false;
        public static string SoundBankDirectory
        {
            get
            {
                return Files.assemblyDir;
            }
        }

        public static void Init()
        {
            if (initialized) return;
            initialized = true;

            //UnityEngine.Debug.Log("AssemblyDir: " + Files.assemblyDir);
            using (Stream manifestResourceStream = new FileStream(SoundBankDirectory +"\\BanditSaoriSoundbank.bnk", FileMode.Open))
            {

                byte[] array = new byte[manifestResourceStream.Length];
                manifestResourceStream.Read(array, 0, array.Length);
                SoundAPI.SoundBanks.Add(array);
            }

            /*AKRESULT akResult = AkSoundEngine.AddBasePath(SoundBankDirectory);

            AkSoundEngine.LoadBank("BanditSaoriSoundbank.bnk", out _);*/
        }
    }
}

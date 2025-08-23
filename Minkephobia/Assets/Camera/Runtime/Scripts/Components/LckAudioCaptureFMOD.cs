using System.Runtime.InteropServices;

namespace Liv.Lck
{
    internal class LckAudioCaptureFMOD : LckAudioCapture
    {

#if QCK_FMOD
        private FMOD.DSP_READCALLBACK mReadCallback;
        private FMOD.DSP mCaptureDSP;
        public static int samplerate = 0;
#endif
        private GCHandle mObjHandle;
        private int mChannels;


#if QCK_FMOD
        [AOT.MonoPInvokeCallback(typeof(FMOD.DSP_READCALLBACK))]
        static FMOD.RESULT CaptureDSPReadCallback(ref FMOD.DSP_STATE dsp_state, IntPtr inbuffer, IntPtr outbuffer, uint length, int inchannels, ref int outchannels)
        {
            FMOD.DSP_STATE_FUNCTIONS functions = (FMOD.DSP_STATE_FUNCTIONS)Marshal.PtrToStructure(dsp_state.functions, typeof(FMOD.DSP_STATE_FUNCTIONS));

            IntPtr userData;
            functions.getuserdata(ref dsp_state, out userData);
            functions.getsamplerate(ref dsp_state, ref samplerate);

            GCHandle objHandle = GCHandle.FromIntPtr(userData);
            AudioCaptureFMOD obj = objHandle.Target as AudioCaptureFMOD;

            if (inchannels >= obj.mChannels)
            {
                float[] tmp = new float[length * inchannels];
                Marshal.Copy(inbuffer, tmp, 0, tmp.Length);
                Marshal.Copy(tmp, 0, outbuffer, tmp.Length);

                if (obj.captureAudio)
                {
                    float[] buffer = new float[length * obj.mChannels];
                    for (int i = 0; i < length; i++)
                        for (int ch = 0; ch < obj.mChannels; ch++)
                            buffer[i * obj.mChannels + ch] = tmp[i * inchannels + ch];
                    lock (obj) obj.audioFrames.Add(buffer);
                }
            }

            return FMOD.RESULT.OK;
        }
#endif

        // Start is called before the first frame update
        void Start()
        {
#if QCK_FMOD
            // Assign the callback to a member variable to avoid garbage collection
            mReadCallback = CaptureDSPReadCallback;

            // Allocate a data buffer large enough for 8 channels
            uint bufferLength;
            int numBuffers;
            FMODUnity.RuntimeManager.CoreSystem.getDSPBufferSize(out bufferLength, out numBuffers);
            mChannels = 2;

            // Get a handle to this object to pass into the callback
            mObjHandle = GCHandle.Alloc(this);
            if (mObjHandle != null)
            {
                // Define a basic DSP that receives a callback each mix to capture audio
                FMOD.DSP_DESCRIPTION desc = new FMOD.DSP_DESCRIPTION();
                desc.numinputbuffers = 1;
                desc.numoutputbuffers = 1;
                desc.read = mReadCallback;
                desc.userdata = GCHandle.ToIntPtr(mObjHandle);

                // Create an instance of the capture DSP and attach it to the master channel group to capture all audio
                FMOD.ChannelGroup masterCG;
                if (FMODUnity.RuntimeManager.CoreSystem.getMasterChannelGroup(out masterCG) == FMOD.RESULT.OK)
                {
                    if (FMODUnity.RuntimeManager.CoreSystem.createDSP(ref desc, out mCaptureDSP) == FMOD.RESULT.OK)
                    {
                        if (masterCG.addDSP(0, mCaptureDSP) != FMOD.RESULT.OK)
                        {
                            LckLog.LogWarningFormat("LCK FMOD: Unable to add mCaptureDSP to the master channel group");
                        }
                    }
                    else
                    {
                        LckLog.LogWarningFormat("LCK FMOD: Unable to create a DSP: mCaptureDSP");
                    }
                }
                else
                {
                    LckLog.LogWarningFormat("LCK FMOD: Unable to create a master channel group: masterCG");
                }
            }
            else
            {
                LckLog.LogWarningFormat("LCK FMOD: Unable to create a GCHandle: mObjHandle");
            }
#endif
        }

        void OnDestroy()
        {
#if QCK_FMOD
            if (mObjHandle != null)
            {
                // Remove the capture DSP from the master channel group
                FMOD.ChannelGroup masterCG;
                if (FMODUnity.RuntimeManager.CoreSystem.getMasterChannelGroup(out masterCG) == FMOD.RESULT.OK)
                {
                    if (mCaptureDSP.hasHandle())
                    {
                        masterCG.removeDSP(mCaptureDSP);

                        // Release the DSP and free the object handle
                        mCaptureDSP.release();
                    }
                }
                mObjHandle.Free();
            }
#endif
        }
    }
}

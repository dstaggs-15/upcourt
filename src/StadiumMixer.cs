using System;
using System.Collections.Generic;
namespace Upcourt {
public sealed class StadiumMixer {
    sealed class Voice {public float[] Samples;public int Cursor;public float Gain;public bool Crowd;}
    readonly object gate=new object();readonly List<Voice> voices=new List<Voice>();readonly float[] bed=ArcadeSounds.Get("ambience");
    bool muted,suspended,stadium,paused;float master=.6f,crowd=.75f,effects=.85f,bedLevel;long cursor;
    public bool Muted{get{lock(gate)return muted;}set{lock(gate){muted=value;if(value)voices.Clear();}}}
    public bool Suspended{get{lock(gate)return suspended;}set{lock(gate){suspended=value;if(value)voices.Clear();}}}
    public bool Paused{get{lock(gate)return paused;}set{lock(gate)paused=value;}}
    public bool Stadium{get{lock(gate)return stadium;}set{lock(gate){stadium=value;if(!value)voices.RemoveAll(v=>v.Crowd);}}}
    public float Volume{get{lock(gate)return master;}set{lock(gate)master=Util.Clamp(value,0,1);}}
    public float CrowdVolume{get{lock(gate)return crowd;}set{lock(gate)crowd=Util.Clamp(value,0,1);}}
    public float EffectsVolume{get{lock(gate)return effects;}set{lock(gate)effects=Util.Clamp(value,0,1);}}
    public int VoiceCount{get{lock(gate)return voices.Count;}}
    public void Play(string name){lock(gate){if(muted||suspended)return;
        if(name=="score"){Add("swish",.8f,false);Add("crowd_cheer",.9f,true);return;}
        if(name=="dunk"){Add("rimslam",1,false);Add("crowd_roar",1,true);return;}
        if(name=="fire"){Add("buzzer",.6f,false);Add("crowd_roar",1,true);return;}
        Add(name,1,name.StartsWith("crowd_"));}}
    void Add(string name,float gain,bool isCrowd){
        if(isCrowd){voices.RemoveAll(v=>v.Crowd);if(!stadium)return;}
        if(voices.Count>=20)voices.RemoveAt(0);
        float[] samples=ArcadeSounds.Get(name);
        if(voices.Exists(v=>!v.Crowd&&v.Samples==samples&&v.Cursor<ArcadeSounds.Rate/20))return;
        voices.Add(new Voice{Samples=samples,Gain=gain,Crowd=isCrowd});
    }
    public void Render(short[] output,int frames){if(output==null||frames<0||output.Length<frames*2)throw new ArgumentException("Stereo buffer too small");lock(gate){
        for(int n=0;n<frames;n++){
            float target=stadium&&!muted&&!suspended?(paused?.13f:1):0;bedLevel+=(target-bedLevel)*.0004f;
            int index=(int)(cursor%bed.Length),rightIndex=(index+1731)%bed.Length;
            float left=bed[index]*bedLevel*crowd,right=bed[rightIndex]*bedLevel*crowd;
            for(int i=voices.Count-1;i>=0;i--){Voice v=voices[i];float value=v.Samples[v.Cursor++]*v.Gain*(v.Crowd?crowd:effects);left+=value;right+=value*.94f;if(v.Cursor>=v.Samples.Length)voices.RemoveAt(i);}
            bool silence=muted||suspended;output[n*2]=silence?(short)0:(short)(Math.Tanh(left*master)*26000);output[n*2+1]=silence?(short)0:(short)(Math.Tanh(right*master)*26000);cursor++;
        }
    }}
}
}

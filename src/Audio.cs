using System;
namespace Upcourt {
public sealed class Audio:IDisposable {
    public readonly StadiumMixer Mixer=new StadiumMixer();readonly WaveOutput output;
    public Audio(){if(WaveOutput.DeviceCount>0)output=new WaveOutput(Mixer);}
    public bool Muted {get{return Mixer.Muted;}set{Mixer.Muted=value;}}
    public int Volume {get{return (int)(Mixer.Volume*100);}set{Mixer.Volume=Util.Clamp(value/100f,0,1);}}
    public int CrowdVolume {get{return (int)(Mixer.CrowdVolume*100);}set{Mixer.CrowdVolume=Util.Clamp(value/100f,0,1);}}
    public int EffectsVolume {get{return (int)(Mixer.EffectsVolume*100);}set{Mixer.EffectsVolume=Util.Clamp(value/100f,0,1);}}
    public void SetContext(bool stadium,bool focused,bool paused){Mixer.Stadium=stadium;Mixer.Suspended=!focused;Mixer.Paused=paused;}
    public void Cue(string name){Mixer.Play(name);}
    public void Dispose(){if(output!=null)output.Dispose();}
}
}

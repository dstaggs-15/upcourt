using System;
using System.Collections.Generic;
namespace Upcourt {
public static class ArcadeSounds {
    public const int Rate=22050;static readonly Dictionary<string,float[]> cache=new Dictionary<string,float[]>();static readonly object gate=new object();
    public static float[] Get(string name){lock(gate){float[] result;if(cache.TryGetValue(name,out result))return result;result=name=="ambience"||name.StartsWith("crowd_")?CrowdRecordings.Get(name):CreateEffect(name);cache[name]=result;return result;}}
    static float[] CreateEffect(string name){
        // A jump, cut or pass should not sound like a synthesizer. No pitched glides,
        // artificial crowd hiss, or repeating electronic sneaker loop.
        if(name=="jump"||name=="squeak"||name=="call"||name=="shot"||name=="pass"||name=="swing")return new float[0];
        double seconds=name=="buzzer"?.32:name=="whistle"?.16:name=="menu"?.012:name=="swish"?.11:.13;
        float[] samples=new float[(int)(Rate*seconds)];Random random=new Random(173);double filtered=0,previous=0;
        for(int i=0;i<samples.Length;i++){
            double t=i/(double)Rate,edge=Math.Min(1,t/.003)*Math.Min(1,(seconds-t)/.008),noise=random.NextDouble()*2-1;
            filtered+=(noise-filtered)*.13;double value=0;
            if(name=="bounce")value=(Math.Sin(t*Math.PI*2*76)*.32*Math.Exp(-t*39)+Math.Sin(t*Math.PI*2*182)*.11*Math.Exp(-t*58)+filtered*.13*Math.Exp(-t*150))*edge;
            else if(name=="hit"||name=="steal"||name=="block"||name=="rimslam")value=(Math.Sin(t*Math.PI*2*91)*.22*Math.Exp(-t*48)+filtered*.15*Math.Exp(-t*100))*edge;
            else if(name=="rim")value=(Math.Sin(t*Math.PI*2*413)*.08*Math.Exp(-t*35)+Math.Sin(t*Math.PI*2*1037)*.035*Math.Exp(-t*55))*edge;
            else if(name=="whistle"){double pulse=t<.055||t>.085?1:0;value=(Math.Sin(t*Math.PI*2*2480)+Math.Sin(t*Math.PI*2*2660))*.055*edge*pulse;}
            else if(name=="buzzer")value=(Math.Sin(t*Math.PI*2*110)+Math.Sin(t*Math.PI*2*220)*.3)*.11*edge;
            else if(name=="swish")value=(filtered-previous)*.24*edge;
            else value=Math.Sin(t*Math.PI*2*240)*.025*edge*Math.Exp(-t*140);
            previous=filtered;samples[i]=(float)value;
        }
        return samples;
    }
}
}

using System;
using System.Collections.Generic;
namespace Upcourt {
// Original synthesized arena voices/noise and court effects. No external files.
public static class ArcadeSounds {
    public const int Rate=22050;static readonly Dictionary<string,float[]> cache=new Dictionary<string,float[]>();static readonly object gate=new object();
    public static float[] Get(string name){lock(gate){float[] value;if(cache.TryGetValue(name,out value))return value;value=Create(name);cache[name]=value;return value;}}
    static int Seed(string name){int value=17;foreach(char c in name)value=unchecked(value*31+c);return value;}
    static float[] Create(string name){
        bool crowd=name.StartsWith("crowd_")||name=="ambience";
        double duration=name=="ambience"?8:crowd?2.7:name=="buzzer"?.6:name=="whistle"?.55:.22;
        float[] samples=new float[(int)(Rate*duration)];Random random=new Random(Seed(name));double low=0,high=0,rumble=0;
        for(int i=0;i<samples.Length;i++){
            double t=(double)i/Rate,progress=t/duration,noise=random.NextDouble()*2-1;
            low+=(noise-low)*.24;high+=(noise-high)*.045;rumble+=(noise-rumble)*.008;
            double envelope=Math.Pow(Math.Sin(Math.PI*Math.Min(1,progress)),.6),value;
            if(crowd){
                double voices=0;for(int voice=0;voice<6;voice++){double f=155+voice*31+Math.Sin(t*(1+voice*.13))*14;voices+=Math.Sin(2*Math.PI*f*t)*Math.Sin(t*(3.1+voice*.17)+voice)*.014;}
                double texture=(low-high)*.68+rumble*1.4+voices;
                if(name=="ambience")value=texture*(.46+.08*Math.Sin(t*Math.PI*.5))*(.9+.1*Math.Sin(t*8));
                else if(name=="crowd_boo"||name=="crowd_groan")value=(texture*.5+Math.Sin(t*2*Math.PI*155)*.08+Math.Sin(t*2*Math.PI*192)*.06)*envelope;
                else if(name=="crowd_gasp")value=texture*2.4*Math.Exp(-t*2.6)*Math.Min(1,t*35);
                else value=texture*(name=="crowd_roar"?2.9:1.9)*envelope*(.78+.22*Math.Sin(t*21));
                if(name=="ambience"&&Math.Sin(t*Math.PI*2)>.7)value+=texture*.35;
                if(random.NextDouble()<.002)value+=noise*.18;
            }else{
                double decay=Math.Exp(-t*(name=="rimslam"?15:25));
                if(name=="whistle")value=(Math.Sin(t*Math.PI*2*2700)+Math.Sin(t*Math.PI*2*2830))*.2*envelope;
                else if(name=="buzzer")value=(Math.Sin(t*Math.PI*2*130)+Math.Sin(t*Math.PI*2*260)*.4)*.32*envelope;
                else if(name=="squeak")value=Math.Sin(t*Math.PI*2*(1300+700*Math.Sin(t*19)))*.25*envelope;
                else if(name=="hit"||name=="rimslam"||name=="block")value=(Math.Sin(t*Math.PI*2*(name=="rimslam"?85:120))*.6+noise*.25)*decay;
                else if(name=="swish"||name=="shot"||name=="pass"||name=="swing")value=(low-high)*.85*envelope;
                else if(name=="rim"||name=="bounce")value=Math.Sin(t*Math.PI*2*(190-100*progress))*.5*decay;
                else if(name=="jump"||name=="steal")value=Math.Sin(t*Math.PI*2*(400+progress*700))*.23*envelope;
                else value=Math.Sin(t*Math.PI*2*440)*.15*envelope;
            }
            samples[i]=(float)Math.Tanh(value);
        }
        if(name=="ambience"){int cross=Rate/8;for(int i=0;i<cross;i++){float mix=i/(float)cross;samples[samples.Length-cross+i]=samples[samples.Length-cross+i]*(1-mix)+samples[i]*mix;}}
        return samples;
    }
}
}

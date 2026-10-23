using System;
using System.IO;
using System.Reflection;
namespace Upcourt {
// Baked PCM assets: no codec, network request or loose files needed at runtime.
// Recording authors, sources, edits and CC0 terms: audio/CREDITS.md.
public static class CrowdRecordings {
 public static bool IsCrowd(string cue){return cue=="ambience"||cue=="crowd_cheer"||cue=="crowd_big"||cue=="crowd_first"||cue=="crowd_groan";}
 public static float[] Read(string name){using(Stream stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("Upcourt.Audio."+name+".wav")){if(stream==null)throw new InvalidDataException("Missing crowd recording: "+name);using(BinaryReader reader=new BinaryReader(stream)){if(new string(reader.ReadChars(4))!="RIFF")throw new InvalidDataException("Invalid crowd WAV");reader.ReadInt32();if(new string(reader.ReadChars(4))!="WAVE")throw new InvalidDataException("Invalid crowd WAV");bool format=false;while(stream.Position+8<=stream.Length){string id=new string(reader.ReadChars(4));int size=reader.ReadInt32();long next=stream.Position+size+(size&1);if(size<0||next>stream.Length)throw new InvalidDataException("Invalid crowd WAV size");if(id=="fmt "){format=reader.ReadInt16()==1&&reader.ReadInt16()==1&&reader.ReadInt32()==ArcadeSounds.Rate;reader.ReadInt32();format&=reader.ReadInt16()==2&&reader.ReadInt16()==16;}else if(id=="data"){if(!format||size%2!=0)throw new InvalidDataException("Crowd recording must be mono 16-bit PCM at 22050 Hz");float[] samples=new float[size/2];for(int i=0;i<samples.Length;i++)samples[i]=reader.ReadInt16()/32768f;return samples;}stream.Position=next;}throw new InvalidDataException("Crowd recording has no audio");}}}
 static float[] Reaction(string name,double start,double seconds,float gain){float[] source=Read(name);int offset=(int)(start*ArcadeSounds.Rate),length=Math.Min((int)(seconds*ArcadeSounds.Rate),source.Length-offset);float[] samples=new float[length];for(int i=0;i<length;i++){double t=i/(double)ArcadeSounds.Rate,envelope=Math.Min(1,t/.07)*Math.Min(1,(length-i)/(ArcadeSounds.Rate*.5));samples[i]=source[offset+i]*gain*(float)envelope;}return samples;}
 static float[] CalmAmbience(){float[] source=Read("stadium"),filtered=new float[source.Length];double alpha=1-Math.Exp(-2*Math.PI*900/ArcadeSounds.Rate);double[] stages=new double[4];for(int pass=0;pass<2;pass++)for(int i=0;i<source.Length;i++){double sample=source[i];for(int stage=0;stage<stages.Length;stage++){stages[stage]+=alpha*(sample-stages[stage]);sample=stages[stage];}filtered[i]=(float)sample;}
  // Long, softly changing recorded crowd bed. Remove the piercing tones from the short source loop.
  float[] samples=new float[ArcadeSounds.Rate*35];double energy=0;for(int i=0;i<samples.Length;i++){double t=i/(double)ArcadeSounds.Rate;float blend=(float)(.5+.5*Math.Sin(t*.31));samples[i]=filtered[i%filtered.Length]*(.55f+.2f*blend)+filtered[(i+ArcadeSounds.Rate*3)%filtered.Length]*(.45f-.2f*blend);energy+=samples[i]*samples[i];}float gain=(float)(.012/Math.Max(.00001,Math.Sqrt(energy/samples.Length)));for(int i=0;i<samples.Length;i++)samples[i]=Math.Max(-.09f,Math.Min(.09f,samples[i]*gain));int cross=ArcadeSounds.Rate/4;for(int i=0;i<cross;i++){float blend=i/(float)(cross-1);samples[samples.Length-cross+i]=samples[samples.Length-cross+i]*(1-blend)+samples[i]*blend;}return samples;
 }
 public static float[] Get(string cue){if(cue=="ambience")return CalmAmbience();if(cue=="crowd_roar")return Reaction("cheer",2.4,2.4,.85f);if(cue=="crowd_gasp")return Reaction("applause",.2,1.4,.5f);if(cue=="crowd_boo")return Reaction("groan",.02,2.5,.65f);if(cue=="crowd_cheer")return Reaction("cheer",.2,4.8,1);if(cue=="crowd_big")return Reaction("cheer",2.4,2.4,.85f);if(cue=="crowd_first")return Reaction("applause",.15,3.6,.85f);if(cue=="crowd_groan")return Reaction("groan",.02,2.5,.9f);throw new ArgumentException("Unknown crowd cue");}
}
}




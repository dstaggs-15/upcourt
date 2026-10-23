using System;
using System.IO;
using System.Media;
namespace Upocourt  {
    public class Audio:IDisposable  {
        SoundPlayer player;
        MemoryStream stream;
        public bool Muted;
        public int Volume=60;
        public void Cue(string name) {
            if(Muted||Volume<=0)return;
            try {
                if(player!=null)player.Dispose();
                if(stream!=null)stream.Dispose();
                int rate=22050,count=name=="buzzer"?8000:1800;
                byte[] bytes=new byte[count*2];
                Random random=new Random(7);
                for(int i=0;i<count;i++) {
                    double t=(double)i/rate,f=name=="score"?660:name=="buzzer"?130:360;
                    double wave=Math.Sin(t*f*Math.PI*2)*(1-(double)i/count);
                    if(name=="bounce")wave+=((random.NextDouble()-.5)*.2);
                    short sample=(short)(wave*7000*Volume/100);
                    bytes[i*2]=(byte)sample;
                    bytes[i*2+1]=(byte)(sample>>8);
                }
                stream=new MemoryStream();
                using(var w=new BinaryWriter(stream,System.Text.Encoding.UTF8,true)) {
                    w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                    w.Write(36+bytes.Length);
                    w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                    w.Write(16);
                    w.Write((short)1);
                    w.Write((short)1);
                    w.Write(rate);
                    w.Write(rate*2);
                    w.Write((short)2);
                    w.Write((short)16);
                    w.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                    w.Write(bytes.Length);
                    w.Write(bytes);
                }
                stream.Position=0;
                player=new SoundPlayer(stream);
                player.Play();
            }
            catch(InvalidOperationException) {
            }
            catch(System.ComponentModel.Win32Exception) {
            }
        }
        public void Dispose() {
            if(player!=null)player.Dispose();
            if(stream!=null)stream.Dispose();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
namespace Upcourt {
// Windows' built-in PCM output; no codec pack, media player, or NuGet dependency.
public sealed class WaveOutput:IDisposable {
 [StructLayout(LayoutKind.Sequential,Pack=2)]struct Format {public ushort Tag,Channels;public uint Rate,BytesPerSecond;public ushort Align,Bits,Extra;}
 [StructLayout(LayoutKind.Sequential)]struct Header {public IntPtr Data;public uint Length,Recorded;public UIntPtr User;public uint Flags,Loops;public IntPtr Next;public UIntPtr Reserved;}
 [DllImport("winmm.dll")]static extern uint waveOutGetNumDevs();
 [DllImport("winmm.dll")]static extern uint waveOutOpen(out IntPtr handle,uint device,ref Format format,IntPtr callback,IntPtr instance,uint flags);
 [DllImport("winmm.dll")]static extern uint waveOutPrepareHeader(IntPtr handle,IntPtr header,uint size);
 [DllImport("winmm.dll")]static extern uint waveOutWrite(IntPtr handle,IntPtr header,uint size);
 [DllImport("winmm.dll")]static extern uint waveOutReset(IntPtr handle);
 [DllImport("winmm.dll")]static extern uint waveOutUnprepareHeader(IntPtr handle,IntPtr header,uint size);
 [DllImport("winmm.dll")]static extern uint waveOutClose(IntPtr handle);
 sealed class Buffer {public IntPtr Data,Header;public bool Prepared;}
 readonly List<Buffer> buffers=new List<Buffer>();readonly StadiumMixer mixer;readonly short[] samples=new short[1024];readonly uint headerSize=(uint)Marshal.SizeOf(typeof(Header));readonly int flagOffset=(int)Marshal.OffsetOf(typeof(Header),"Flags");
 IntPtr device;Thread thread;volatile bool stopping;int written;public string Error="";
 public bool Available {get{return device!=IntPtr.Zero&&!stopping&&Error=="";}}public int WrittenBuffers {get{return written;}}
 public static uint DeviceCount {get{try{return waveOutGetNumDevs();}catch{return 0;}}}
 public WaveOutput(StadiumMixer source){mixer=source;try{Format format=new Format();format.Tag=1;format.Channels=2;format.Rate=ArcadeSounds.Rate;format.Bits=16;format.Align=4;format.BytesPerSecond=format.Rate*4;uint result=waveOutOpen(out device,uint.MaxValue,ref format,IntPtr.Zero,IntPtr.Zero,0);if(result!=0)throw new InvalidOperationException("Audio device error "+result);
  for(int i=0;i<3;i++){Buffer b=new Buffer();buffers.Add(b);b.Data=Marshal.AllocHGlobal(samples.Length*2);b.Header=Marshal.AllocHGlobal((int)headerSize);Header header=new Header();header.Data=b.Data;header.Length=(uint)(samples.Length*2);Marshal.StructureToPtr(header,b.Header,false);if(waveOutPrepareHeader(device,b.Header,headerSize)!=0)throw new InvalidOperationException("Audio buffer preparation failed");b.Prepared=true;Write(b);}
  thread=new Thread(Pump);thread.IsBackground=true;thread.Name="Upcourt stadium audio";thread.Start();
 }catch(Exception e){Error=e.Message;Release();}}
 void Write(Buffer b){mixer.Render(samples,samples.Length/2);Marshal.Copy(samples,0,b.Data,samples.Length);uint result=waveOutWrite(device,b.Header,headerSize);if(result!=0)throw new InvalidOperationException("Audio output error "+result);Interlocked.Increment(ref written);}
 void Pump(){try{while(!stopping){foreach(Buffer b in buffers)if((Marshal.ReadInt32(b.Header,flagOffset)&1)!=0)Write(b);Thread.Sleep(6);}}catch(Exception e){Error=e.Message;}}
 void Release(){if(device!=IntPtr.Zero)waveOutReset(device);foreach(Buffer b in buffers){if(b.Prepared&&device!=IntPtr.Zero)waveOutUnprepareHeader(device,b.Header,headerSize);if(b.Header!=IntPtr.Zero)Marshal.FreeHGlobal(b.Header);if(b.Data!=IntPtr.Zero)Marshal.FreeHGlobal(b.Data);}buffers.Clear();if(device!=IntPtr.Zero){waveOutClose(device);device=IntPtr.Zero;}}
 public void Dispose(){if(stopping)return;stopping=true;if(thread!=null)thread.Join();Release();}
}
}




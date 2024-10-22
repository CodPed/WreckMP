using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace WreckMP
{
	internal class NetVehicleAudio
	{
		public bool IsDrivenBySoundController { get; set; }

		public NetVehicleAudio(Transform parent, SoundController ctrl)
		{
			this.ctor(parent, ctrl);
		}

		private void ctor(Transform parent, SoundController ctrl)
		{
			this.controller = ctrl;
			if (this.controller == null)
			{
				Console.LogError("Init " + parent.name + " SoundCOntroller is null", false);
				return;
			}
			if (!parent.gameObject.activeInHierarchy)
			{
				NetVehicleAudio.SoundControllerStart.Invoke(this.controller, null);
			}
			for (int i = 0; i < parent.childCount; i++)
			{
				GameObject gameObject = parent.GetChild(i).gameObject;
				if (gameObject.name == "audio")
				{
					AudioSource component = gameObject.GetComponent<AudioSource>();
					if (component != null)
					{
						this.sources.Add(new NetVehicleAudio.WatchedAudioSource(component));
					}
				}
			}
		}

		public void Update()
		{
			this.controller.enabled = this.IsDrivenBySoundController;
		}

		public bool WriteUpdate(GameEventWriter p, int vehicleHash, bool initSync = false)
		{
			if (p == null)
			{
				return false;
			}
			bool flag = initSync;
			int num = 0;
			while (num < this.sources.Count && !flag)
			{
				if (this.sources[num].HasUpdate)
				{
					flag = true;
				}
				num++;
			}
			if (!flag)
			{
				return false;
			}
			p.Write(7);
			p.Write(vehicleHash);
			for (int i = 0; i < this.sources.Count; i++)
			{
				this.sources[i].WriteUpdates(p, i, initSync);
			}
			p.Write(247);
			return true;
		}

		public SoundController controller;

		public List<NetVehicleAudio.WatchedAudioSource> sources = new List<NetVehicleAudio.WatchedAudioSource>();

		private static MethodInfo SoundControllerStart = typeof(SoundController).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic);

		internal class WatchedAudioSource
		{
			public bool HasUpdate
			{
				get
				{
					return this.lastPlaying != this.src.isPlaying || this.lastVolume != this.src.volume || this.lastPitch != this.src.pitch;
				}
			}

			public WatchedAudioSource(AudioSource src)
			{
				this.src = src;
				this.lastPlaying = src.isPlaying;
				this.lastVolume = src.volume;
				this.lastPitch = src.pitch;
			}

			public void WriteUpdates(GameEventWriter p, int srcIndex, bool initSync = false)
			{
				if (!this.HasUpdate && !initSync)
				{
					return;
				}
				p.Write(15);
				p.Write(srcIndex);
				if (this.lastPlaying != this.src.isPlaying || initSync)
				{
					p.Write(31);
					p.Write(this.src.isPlaying);
					if (this.src.isPlaying)
					{
						p.Write(this.src.time);
					}
					this.lastPlaying = this.src.isPlaying;
				}
				if (this.lastVolume != this.src.volume || initSync)
				{
					p.Write(47);
					p.Write(this.src.volume);
					this.lastVolume = this.src.volume;
				}
				if (this.lastPitch != this.src.pitch || initSync)
				{
					p.Write(63);
					p.Write(this.src.pitch);
					this.lastPitch = this.src.pitch;
				}
				p.Write(byte.MaxValue);
			}

			public void OnUpdate(bool? isPlaying, float? time, float? volume, float? pitch)
			{
				if (isPlaying != null)
				{
					if (isPlaying.Value)
					{
						this.src.Play();
						if (time != null)
						{
							this.src.time = time.Value;
						}
					}
					else
					{
						this.src.Stop();
					}
				}
				if (volume != null)
				{
					this.src.volume = volume.Value;
				}
				if (pitch != null)
				{
					this.src.pitch = pitch.Value;
				}
			}

			private AudioSource src;

			private bool lastPlaying;

			private float lastVolume = 1f;

			private float lastPitch = 1f;
		}
	}
}

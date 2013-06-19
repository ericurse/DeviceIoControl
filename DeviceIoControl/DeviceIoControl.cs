﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using AlphaOmega.Debug.Native;

namespace AlphaOmega.Debug
{
	/// <summary>Device info</summary>
	public class DeviceIoControl : IDisposable
	{
		private readonly IntPtr _deviceHandle;
		private readonly Byte? _deviceId;
		private readonly String _deviceName;
		private Boolean _disposed = false;

		private Disc _disc;
		private Storage _storage;
		private Volume _volume;
		private FileSystem _fs;

		/// <summary>Opened device handle</summary>
		private IntPtr Handle { get { return this._deviceHandle; } }
		/// <summary>Device ID</summary>
		public Byte? ID { get { return this._deviceId; } }
		/// <summary>Name of the device</summary>
		public String Name { get { return this._deviceName; } }
		/// <summary>Disc IO commands</summary>
		public Disc Disc
		{
			get
			{
				if(this._disc == null)
					this._disc = new Disc(this);
				return this._disc;
			}
		}
		/// <summary>Storage IO commands</summary>
		public Storage Storage
		{
			get
			{
				if(this._storage == null)
					this._storage = new Storage(this);
				return this._storage;
			}
		}
		/// <summary>Volume IO commands</summary>
		public Volume Volume
		{
			get
			{
				if(this._volume == null)
					this._volume = new Volume(this);
				return this.Volume;
			}
		}
		/// <summary>File system IO commands</summary>
		/// <remarks>FSCTL can be null if device opened by ID</remarks>
		public FileSystem FileSystem
		{
			get
			{
				if(this._fs == null && this._deviceId == null)
					this._fs = new FileSystem(this);
				return this._fs;
			}
		}
		/// <summary>Get device power state</summary>
		public Boolean IsDeviceOn
		{
			get
			{
				Boolean isOn = false;
				if(!Methods.GetDevicePowerState(this.Handle, out isOn))
					throw new Win32Exception();
				else
					return isOn;
			}
		}

		/// <summary>Create instance of device info class by drive letter</summary>
		public DeviceIoControl(String deviceName)
			: this(null, deviceName)
		{
		}
		/// <summary>Create instance of device info class by device ID</summary>
		/// <param name="deviceId">Device ID</param>
		[Obsolete("Часть функционала работать не будет", false)]
		public DeviceIoControl(Byte deviceId)
			: this(deviceId, null)
		{
		}
		/// <summary>Create instance of device info class by device ID or drive letter</summary>
		/// <param name="deviceId">ID of device</param>
		/// <param name="deviceName">name of device</param>
		public DeviceIoControl(Byte? deviceId, String deviceName)
		{
			this._deviceId = deviceId;

			if(deviceId.HasValue)
				this._deviceName = DeviceIoControl.GetDeviceName(deviceId.Value);
			else if(!String.IsNullOrEmpty(deviceName))
			{
				Char deviceLetter = Array.Find(deviceName.ToCharArray(), delegate(Char ch) { return Char.IsLetter(ch); });
				this._deviceName = String.Format(Constant.DriveWinNTArg1, deviceLetter);
			} else
				throw new ArgumentNullException();

			this._deviceHandle = Methods.OpenDevice(this.Name);

			//Получить серийный номер USB
			/*StorageAPI.MEDIA_SERIAL_NUMBER_DATA sn = this.DeviceIoControl<StorageAPI.MEDIA_SERIAL_NUMBER_DATA>(
				Constant.IOCTL_STORAGE.GET_MEDIA_SERIAL_NUMBER,
				null);*/

			//Получить информацию по устройству через ATA
			//http://www.osronline.com/showthread.cfm?link=133005
			//http://stackoverflow.com/questions/5070987/sending-ata-commands-directly-to-device-in-windows
			/*AtaAPI.ATA_PASS_THROUGH_EX ataCmd = new AtaAPI.ATA_PASS_THROUGH_EX();
			ataCmd.Length = (UInt16)System.Runtime.InteropServices.Marshal.SizeOf(typeof(AtaAPI.ATA_PASS_THROUGH_EX));
			ataCmd.DataBufferOffset = (UInt32)System.Runtime.InteropServices.Marshal.SizeOf(typeof(AtaAPI.ATA_PASS_THROUGH_EX));
			ataCmd.DataTransferLength = 512;
			ataCmd.AtaFlags = AtaAPI.ATA_PASS_THROUGH_EX.AtaPassThroughFlags.DATA_IN;
			ataCmd.TimeOutValue = 2;
			ataCmd.PreviousTaskFile = new Byte[8];
			ataCmd.CurrentTaskFile = new Byte[8];
			ataCmd.CurrentTaskFile[6] = 0xec;

			AtaAPI.ATA_PASS_THROUGH_EX result = this.DeviceIoControl<AtaAPI.ATA_PASS_THROUGH_EX>(
				Constant.IOCTL_ATA.PASS_THROUGH,
				ataCmd);*/
		}
		/// <summary>Sends a control code directly to a specified device driver, causing the corresponding device to perform the corresponding operation.</summary>
		/// <typeparam name="T">Return type</typeparam>
		/// <param name="dwIoControlCode">The control code for the operation. This value identifies the specific operation to be performed and the type of device on which to perform it.</param>
		/// <param name="inParams">A pointer to the input buffer that contains the data required to perform the operation.</param>
		/// <returns>Return type</returns>
		public T IoControl<T>(UInt32 dwIoControlCode, Object inParams) where T : struct
		{
			UInt32 bytesReturned;
			return this.IoControl<T>(dwIoControlCode, inParams, out bytesReturned);
		}
		/// <summary>Sends a control code directly to a specified device driver, causing the corresponding device to perform the corresponding operation.</summary>
		/// <typeparam name="T">Return type</typeparam>
		/// <param name="dwIoControlCode">The control code for the operation. This value identifies the specific operation to be performed and the type of device on which to perform it.</param>
		/// <param name="inParams">A pointer to the input buffer that contains the data required to perform the operation.</param>
		/// <param name="bytesReturned">A pointer to a variable that receives the size of the data stored in the output buffer, in bytes.</param>
		/// <returns>Return object</returns>
		public T IoControl<T>(UInt32 dwIoControlCode, Object inParams, out UInt32 bytesReturned) where T : struct
		{
			return Methods.DeviceIoControl<T>(
				this.Handle,
				dwIoControlCode,
				inParams,
				out bytesReturned);
		}
		/// <summary>Sends a control code directly to a specified device driver, causing the corresponding device to perform the corresponding operation.</summary>
		/// <typeparam name="T">Return type</typeparam>
		/// <param name="dwIoControlCode">The control code for the operation. This value identifies the specific operation to be performed and the type of device on which to perform it.</param>
		/// <param name="inParams">A pointer to the input buffer that contains the data required to perform the operation.</param>
		/// <param name="outParams">Return object</param>
		/// <returns>Result of method execution</returns>
		public Boolean IoControl<T>(UInt32 dwIoControlCode, Object inParams, out T outParams) where T : struct
		{
			UInt32 bytesReturned;
			return this.IoControl<T>(dwIoControlCode, inParams, out bytesReturned, out outParams);
		}
		/// <summary>Sends a control code directly to a specified device driver, causing the corresponding device to perform the corresponding operation.</summary>
		/// <typeparam name="T">Return type</typeparam>
		/// <param name="dwIoControlCode">The control code for the operation. This value identifies the specific operation to be performed and the type of device on which to perform it.</param>
		/// <param name="inParams">A pointer to the input buffer that contains the data required to perform the operation.</param>
		/// <param name="bytesReturned">A pointer to a variable that receives the size of the data stored in the output buffer, in bytes.</param>
		/// <param name="outParams">Return object</param>
		/// <returns>Result of method execution</returns>
		public Boolean IoControl<T>(UInt32 dwIoControlCode, Object inParams, out UInt32 bytesReturned, out T outParams) where T : struct
		{
			return Methods.DeviceIoControl<T>(
				this.Handle,
				dwIoControlCode,
				inParams,
				out bytesReturned,
				out outParams);
		}
		/// <summary>Get all logical devices</summary>
		/// <returns>Drive name and type</returns>
		public static IEnumerable<KeyValuePair<String, WinAPI.DRIVE>> GetLogicalDrives()
		{
			foreach(String drive in Environment.GetLogicalDrives())
				yield return new KeyValuePair<String, WinAPI.DRIVE>(drive, Methods.GetDriveTypeA(drive));
		}
		/// <summary>Dispose device info and close all managed handles</summary>
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		private void Dispose(Boolean disposing)
		{
			if(!this._disposed
				&& this.Handle != IntPtr.Zero && this.Handle != Constant.INVALID_HANDLE_VALUE)
			{
				if(Methods.CloseHandle(this._deviceHandle))
					this._disposed = true;
				else throw new Win32Exception();
			}
		}
		/// <summary>Destructor to close native handle</summary>
		~DeviceIoControl()
		{
			this.Dispose(false);
		}
		/// <summary>Get system device name</summary>
		/// <param name="deviceId">ID of device</param>
		/// <returns>System device name</returns>
		private static String GetDeviceName(Byte deviceId)
		{
			String result = Constant.DeviceWin32;

			if(Environment.OSVersion.Platform == PlatformID.Win32NT)
				result = String.Format(Constant.DeviceWinNTArg1, deviceId);

			return result;
		}
	}
}
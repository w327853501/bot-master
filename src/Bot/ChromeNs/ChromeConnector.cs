using BotLib;
using BotLib.BaseClass;
using BotLib.Misc;
using Bot.Automation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bot.ChromeNs
{
	public abstract class ChromeConnector : Disposable
	{
		public const bool IsBanChromeForTest = false;
		public readonly NoReEnterTimer Timer;
		private bool _isDisposed;
		private ManualResetEventSlim _evslim;
		private bool _isChromeOk;
		private string _logMark;
		protected int _hwnd;
		protected string _siUrl;
		private ChromeOperator _chromeOperator;
		private int _continuousInitSessionCount;
		public event EventHandler<ChromeAdapterEventArgs> EvChromeDetached;
		public event EventHandler<ChromeAdapterEventArgs> EvChromeConnected;
		public bool IsChromeOk
		{
			get
			{
				if (_isChromeOk)
				{
					_isChromeOk = (ChromOp != null && !ChromOp.IsDisposed);
				}
				return _isChromeOk;
			}
			private set
			{
				if (value)
				{
					_evslim.Set();
				}
				else
				{
					_evslim.Reset();
				}
				if (_isChromeOk != value)
				{
					Log.Info("IsOcrOk=" + value.ToString() + "," + _logMark);
					_isChromeOk = value;
				}
			}
		}

		protected ChromeOperator ChromOp
		{
			get
			{
				return _chromeOperator;
			}
			set
			{
				var chromeOp = _chromeOperator;
				_chromeOperator = value;
                if (chromeOp != null)
				{
                    chromeOp.Dispose();
					if (EvChromeDetached != null)
					{
						EvChromeDetached(this, new ChromeAdapterEventArgs
						{
							Connector = this
						});
					}
				}
			}
		}
		public bool WaitForChromeOk(int timeoutMs = 1000)
		{
			bool rt;
			if (IsChromeOk)
			{
				rt = true;
			}
			else
			{
				if (!(rt = _evslim.WaitHandle.WaitOne(timeoutMs)))
				{
					Log.Error("cc,等待超时," + _logMark);
				}
			}
			return rt;
		}
		protected ChromeConnector(int hwnd, string logMark)
		{
			_isDisposed = false;
			_evslim = new ManualResetEventSlim(false);
			_isChromeOk = false;
			_logMark = "Ocr";
			_continuousInitSessionCount = 0;
			_logMark = logMark;
			ClearStateValues();
			_hwnd = hwnd;
			_siUrl = ChromeOperator.GetSessionInfoUrl(hwnd);
			Task.Factory.StartNew(ListenService);
			Timer = new NoReEnterTimer(ReconnectLoop, 2000, 1000);
		}
		protected override void CleanUp_Managed_Resources()
		{
			if (Timer != null)
			{
				Timer.Dispose();
			}
			if (ChromOp != null)
			{
				ChromOp.Dispose();
			}
			IsChromeOk = false;
		}
		protected virtual void ClearStateValues()
		{
			IsChromeOk = false;
		}
		protected abstract ChromeOperator CreateChromeOperator(string chromeSessionInfoUrl);
		private void ReconnectLoop()
		{
			try
			{
				if (_isDisposed)
				{
					Timer.Dispose();
					Log.Info(_logMark + ",timer closed.");
				}
				else
				{
					if (!IsChromeOk)
					{
						ListenService();
					}
				}
			}
			catch (Exception e)
			{
				Log.Exception(e);
			}
		}
		private void ListenService()
		{
			SkipIfExcuting.Excute(InitServiceInner);
		}
		private void InitServiceInner()
		{
			try
			{
				ClearStateValues();
				WaitForInit();
				if (!_isDisposed)
				{
					if (!WinApi.IsHwndAlive(_hwnd))
					{
						Log.Info("窗口已关闭，" + _logMark + "关闭");
						_isDisposed = true;
					}
					else
					{
						Log.Info("开始初始化" + _logMark + "...");
						ChromOp = CreateChromeOperator(_siUrl);
						if (ChromOp == null)
						{
							throw new Exception("无法获取  operator," + _logMark);
						}
						ChromOp.ListenChromeDetachedTurbo(Detached);
						_continuousInitSessionCount = 0;
						IsChromeOk = true;
						Timer.AddAction(ChromOp.VerifySessionAlive, 2000, 0);
						if (EvChromeConnected != null)
						{
							EvChromeConnected(this, new ChromeAdapterEventArgs
							{
								Connector = this
							});
						}
						Log.Info(_logMark + "初始化成功！");
					}
				}
			}
			catch (Exception ex)
			{
				ClearStateValues();
				_continuousInitSessionCount++;
				if (_continuousInitSessionCount > 2)
				{
					ChromOp = null;
				}
				Log.Error(_logMark + "初始化出错，原因=" + ex.Message);
			}
			finally
			{
				Log.Info("结束初始化" + _logMark);
			}
		}
		private void WaitForInit()
		{
			int ms = 0;
			if (_continuousInitSessionCount > 0)
			{
				ms = ((_continuousInitSessionCount > 2) ? 2000 : 500);
			}
			if (ms > 0)
			{
				Thread.Sleep(ms);
			}
		}
		private void Detached(string reason)
		{
			Log.Info("!!!!!!!!!!!!!!" + _logMark + " Detached,Reason=" + reason);
			ClearStateValues();
			ChromOp = null;
		}
	}
}

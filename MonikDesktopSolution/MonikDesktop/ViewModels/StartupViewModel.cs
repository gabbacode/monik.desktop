﻿using System;
using System.Threading.Tasks;
using System.Windows;
using Autofac;
using Doaking.Core.Oak;
using MonikDesktop.Common;
using MonikDesktop.Common.Interfaces;
using MonikDesktop.Properties;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace MonikDesktop.ViewModels
{
	public class StartupViewModel : ReactiveObject, IStartupWindow
	{
		private readonly Shell _shell;

		public StartupViewModel(IOakApplication aApp, Shell aShell)
		{
			_shell = aShell;
			App = aApp;

			Title = "App settings";

			App.ObservableForProperty(x => x.ServerUrl)
				.Subscribe(v =>
				{
					Settings.Default.ServerUrl = v.Value;
					Settings.Default.Save();
				});

			var canNew = App.WhenAny(x => x.ServerUrl, x => !string.IsNullOrWhiteSpace(x.Value));
			NewLogCommand = ReactiveCommand.Create(NewLog, canNew);
			NewKeepAliveCommand = ReactiveCommand.Create(NewKeepAlive, canNew);
		    ShowSpinnerCommand = ReactiveCommand.Create(ChangeSpinnerVisibility, canNew);
		}

		public IOakApplication App { get; }

		public ReactiveCommand NewLogCommand { get; set; }
	    public ReactiveCommand NewKeepAliveCommand { get; set; }
	    public ReactiveCommand ShowSpinnerCommand { get; set; }

        [Reactive]
		public bool CanClose { get; set; } = false;

		[Reactive]
		public ReactiveCommand CloseCommand { get; set; } = null;

		[Reactive]
		public string Title { get; set; }

	    private async Task NewLog()
	    {
	        // TODO: check server url

	        ShowSpinner = Visibility.Visible;

	        ILogsWindow       log     = null;
	        IPropertiesWindow props   = null;
	        ISourcesWindow    sources = null;
	        ILogDescription   desc    = null;

	        await Task.Run(() =>
	        {
	            log     = _shell.Resolve<ILogsWindow>();
	            props   = _shell.Resolve<IPropertiesWindow>();
	            sources = _shell.Resolve<ISourcesWindow>();
	            desc    = _shell.Resolve<ILogDescription>();
	        });
            
	        _shell.ShowDocument(log);
	        _shell.ShowTool(props);
	        _shell.ShowTool(sources);
	        _shell.ShowTool(desc);

	        _shell.SelectedWindow = log;

	        ShowSpinner = Visibility.Collapsed;
	    }

	    private async Task NewKeepAlive()
	    {
	        // TODO: check server url

	        ShowSpinner = Visibility.Visible;

	        IKeepAliveWindow  keepAlive = null;
	        IPropertiesWindow props     = null;
	        ISourcesWindow    sources   = null;
	        ILogDescription   desc      = null;

	        await Task.Run(() =>
	        {
	            keepAlive = _shell.Resolve<IKeepAliveWindow>();
	            props     = _shell.Resolve<IPropertiesWindow>();
	            sources   = _shell.Resolve<ISourcesWindow>();
	            desc      = _shell.Resolve<ILogDescription>();
	        });
            
	        _shell.ShowDocument(keepAlive);
	        _shell.ShowTool(props);
	        _shell.ShowTool(sources);
	        _shell.ShowTool(desc);

	        _shell.SelectedWindow = keepAlive;

	        ShowSpinner = Visibility.Collapsed;
	    }

	    private async Task ChangeSpinnerVisibility()
	    {
	    }


	    [Reactive]
        public Visibility ShowSpinner { get; set; } = Visibility.Collapsed;
	}
}
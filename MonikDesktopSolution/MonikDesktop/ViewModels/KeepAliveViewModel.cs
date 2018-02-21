﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows;
using Doaking.Core.Oak;
using MonikDesktop.Common.Enums;
using MonikDesktop.Common.Interfaces;
using MonikDesktop.Common.ModelsApi;
using MonikDesktop.Common.ModelsApp;
using MonikDesktop.ViewModels.ShowModels;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace MonikDesktop.ViewModels
{
	public class KeepAliveViewModel : ReactiveObject, IKeepAliveWindow
	{
		private readonly IMonikService _service;
		private readonly ISourcesCache _cache;

		private readonly KeepAliveModel _model;
		
		private IDisposable _updateExecutor;

		public KeepAliveViewModel(Shell aShell, IMonikService aService, ISourcesCache aCache)
		{
			_service = aService;
			_cache = aCache;

			KeepALiveList = new ReactiveList<KeepALiveItem>();
			_model = new KeepAliveModel { Caption = "KeepAlives"};

			_model.WhenAnyValue(x => x.Caption, x => x.Online)
				.Subscribe(v => Title = v.Item1 + (v.Item2 ? " >" : " ||"));

			var canStart = _model.WhenAny(x => x.Online, x => !x.Value);
			StartCommand = ReactiveCommand.Create(OnStart, canStart);

			var canStop = _model.WhenAny(x => x.Online, x => x.Value);
			StopCommand = ReactiveCommand.Create(OnStop, canStop);

			UpdateCommand = ReactiveCommand.Create(OnUpdate, canStop);
			UpdateCommand.Subscribe(results =>
			{
                KeepALiveList.Clear();
			    KeepALiveList.AddRange(results);
			});

			UpdateCommand.ThrownExceptions
				.Subscribe(ex =>
				{
					// TODO: handle
				});
		}

		// TODO: alert if receivedtime < createdtime or receivedtime >> createdtime
		public ReactiveList<KeepALiveItem> KeepALiveList { get; set; }
		public ReactiveCommand<Unit, KeepALiveItem[]> UpdateCommand { get; set; }

		public long? LastId { get; private set; }

		public ShowModel Model => _model;

		public ReactiveCommand StartCommand { get; set; }
		public ReactiveCommand StopCommand { get; set; }

		[Reactive] public string          Title            { get; set; }
        [Reactive] public bool            CanClose         { get; set; } = true;
        [Reactive] public ReactiveCommand CloseCommand     { get; set; } = null;
	    [Reactive] public bool            WindowIsEbabled  { get; set; } = true;

	    private void OnStart()
		{
			LastId = null;
			KeepALiveList.Clear();

			var interval = TimeSpan.FromSeconds(_model.RefreshSec);
			_updateExecutor = Observable
				.Timer(interval, interval)
				.Select(_ => Unit.Default)
				.InvokeCommand(UpdateCommand);

			Model.Online = true;
		}

		private void OnStop()
		{
			if (_updateExecutor == null)
				return;

			_updateExecutor.Dispose();
			_updateExecutor = null;

			_model.Online = false;
		}

		private KeepALiveItem[] OnUpdate()
		{
			var req = new EKeepAliveRequest();

			EKeepAlive_[] response;

			try
			{
				response = _service.GetKeepAlives(req);
			}
			catch
			{
			    return new[]
			    {
			        new KeepALiveItem
			        {
			            Instance = new Instance
			            {
			                ID = -1,
			                Name = "INTERNAL",
			                Source = new Source {ID = -1, Name = "ERROR"}
			            },
			            Created = DateTime.Now
			        }
			    };
			}

			if (response.Length > 0)
				LastId = response.Max(x => x.ID);

		    response = response.
		        GroupBy(x => _cache.GetInstance(x.InstanceID).Name).OrderBy(g => g.Key).
		        SelectMany(g => g.OrderBy(x => _cache.GetInstance(x.InstanceID).Source.Name)).ToArray();

            var result = response.Select(x =>
			{
			    var created = x.Created.ToLocalTime();
			    var statusIsOk = (DateTime.Now - x.Created.ToLocalTime()).TotalSeconds < _model.KeepAliveWarningPeriod;
			    return new KeepALiveItem
                {
                    Created    = created,
                    CreatedStr = created.ToString(_model.DateTimeFormat),
			        Instance   = _cache.GetInstance(x.InstanceID),
                    StatusIsOk = statusIsOk,
                    Status     = statusIsOk?"OK": "ERROR"
                };
			});

			return result.ToArray();
		}
    }
}
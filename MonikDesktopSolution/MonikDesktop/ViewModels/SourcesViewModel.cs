﻿using DynamicData;
using DynamicData.Binding;
using MonikDesktop.Common;
using MonikDesktop.Common.Interfaces;
using MonikDesktop.Common.ModelsApp;
using MonikDesktop.ViewModels.ShowModels;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Ui.Wpf.Common;
using Ui.Wpf.Common.ViewModels;

namespace MonikDesktop.ViewModels
{
    public class SourcesViewModel : ViewModelBase
    {
        private WithSourcesShowModel _model;

        public SourcesViewModel(IShell shell)
        {
            Title = "Sources";

            shell.WhenAnyValue(x => x.SelectedView)
                .Where(v => !(v is IToolView))
                .Subscribe(v =>
                {
                    if (v is IShowView showView &&
                        showView.ShowViewModel.Model is WithSourcesShowModel model)
                    {
                        UpdateModel(model);
                        IsEnabled = true;
                    }
                    else
                        IsEnabled = false;
                });

            this.ObservableForProperty(x => x.SelectedItem)
                .Where(v => v.Value != null)
                .Subscribe(v => SelectedHack = v.Value);
        }

        [Reactive] public ReactiveCommand<Unit, Unit> SelectNoneCommand { get; set; }
        [Reactive] public ReactiveCommand<Unit, Unit> SelectGroupCommand { get; set; }

        private IDisposable _cleanupRefresh;
        private bool _syncCheckStatusesInProcess;
        private IDisposable _itemsSubscription;
        [Reactive] public ReadOnlyObservableCollection<SourceItem> Items { get; set; }

        [Reactive] public SourceItem SelectedHack { get; set; }
        [Reactive] public SourceItem SelectedItem { get; set; }
        [Reactive] public string FilterText { get; set; }

        private void SetChecked(Func<SourceItem, bool> condition)
        {
            _model.Cache.SourceItems.Edit(innerCache =>
            {
                foreach (var it in innerCache.Items)
                {
                    var val = condition(it);
                    if (it.Checked != val)
                        it.Checked = val;
                }
            });
        }

        private void SelectNone()
        {
            _model.SelectedSourcesClear();
        }

        private void SelectGroup()
        {
            if (SelectedHack != null && !_model.Groups.Contains(SelectedHack.GroupID))
                _model.SelectSourcesGroup(SelectedHack.GroupID);
        }

        private void Reset()
        {
            _itemsSubscription?.Dispose();
            FilterText = "";

            var dynamicFilter = this.WhenAnyValue(x => x.FilterText)
                .Throttle(TimeSpan.FromSeconds(0.7), RxApp.MainThreadScheduler)
                .Select(Filters.CreateFilterSourceItem);

            var itemsSubscription = _model.Cache.SourceItems
                .Connect()
                .Filter(dynamicFilter)
                .Sort(SortExpressionComparer<SourceItem>
                    .Ascending(x => x.GroupName)
                    .ThenByAscending(x => x.SourceName)
                    .ThenByAscending(x => x.InstanceName))
                .ObserveOnDispatcher()
                .Bind(out var items)
                .Subscribe();

            Items = items;

            var refreshSubscription = _model.Cache.SourceItems
                .Connect()
                .WhenPropertyChanged(x => x.Checked)
                .Where(_ => !_syncCheckStatusesInProcess)
                .Subscribe(x => _model.OnSourceItemChanged(x.Sender));

            _itemsSubscription = Disposable.Create(() =>
            {
                itemsSubscription.Dispose();
                refreshSubscription.Dispose();
            });
        }

        private void SyncCheckStatuses()
        {
            _syncCheckStatusesInProcess = true;
            SetChecked(x => _model.Groups.Contains(x.GroupID) || _model.Instances.Contains(x.InstanceID));
            _syncCheckStatusesInProcess = false;
        }

        private void UpdateModel(WithSourcesShowModel model)
        {
            if (model == _model)
                return;

            var cacheIsChanged = _model == null || model == null || _model.Cache != model.Cache;
            _model = model;
            Refresh(cacheIsChanged);
        }

        private void Refresh(bool cacheIsChanged)
        {
            SelectedItem = null;
            SelectedHack = null;

            _cleanupRefresh?.Dispose();

            if (cacheIsChanged)
                Reset();

            var publish = _model.SelectedSourcesChanged
                .StartWith(true)
                .Publish();

            var syncSubscription = publish
                .Subscribe(needSync =>
                {
                    if (needSync)
                        SyncCheckStatuses();
                });

            // Select None Command
            var canSelectNone = publish
                .Select(_ => _model.Instances.Count > 0 || _model.Groups.Count > 0);

            SelectNoneCommand = ReactiveCommand.Create(SelectNone, canSelectNone);

            // Select Group Command
            var canSelectGroup = this.WhenAny(x => x.SelectedHack, v => v.Value)
                .Merge(_model.SelectedSourcesChanged.Select(_ => SelectedHack))
                .Select(si => si != null && !_model.Groups.Contains(si.GroupID));

            SelectGroupCommand = ReactiveCommand.Create(SelectGroup, canSelectGroup);

            var connection = publish.Connect();

            _cleanupRefresh = Disposable.Create(() =>
            {
                SelectNoneCommand?.Dispose();
                SelectGroupCommand?.Dispose();
                syncSubscription?.Dispose();
                connection?.Dispose();
            });
        }
    } //end of class
}
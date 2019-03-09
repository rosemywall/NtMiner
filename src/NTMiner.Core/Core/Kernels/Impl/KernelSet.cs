﻿using NTMiner.Repositories;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NTMiner.Core.Kernels.Impl {
    internal class KernelSet : IKernelSet {
        private readonly INTMinerRoot _root;
        private readonly Dictionary<Guid, KernelData> _dicById = new Dictionary<Guid, KernelData>();

        private readonly bool _isUseJson;
        public KernelSet(INTMinerRoot root, bool isUseJson) {
            _root = root;
            _isUseJson = isUseJson;
            VirtualRoot.Accept<RefreshKernelSetCommand>(
                "处理刷新内核数据集命令",
                LogEnum.Console,
                action: message => {
                    IRepository<KernelData> repository = NTMinerRoot.CreateServerRepository<KernelData>(isUseJson);
                    foreach (var item in repository.GetAll()) {
                        if (_dicById.ContainsKey(item.Id)) {
                            VirtualRoot.Execute(new UpdateKernelCommand(item));
                        }
                        else {
                            VirtualRoot.Execute(new AddKernelCommand(item));
                        }
                    }
                }).AddToCollection(root.ContextHandlers);
            VirtualRoot.Accept<AddKernelCommand>(
                "添加内核",
                LogEnum.Console,
                action: message => {
                    InitOnece();
                    if (message == null || message.Input == null || message.Input.GetId() == Guid.Empty) {
                        throw new ArgumentNullException();
                    }
                    if (string.IsNullOrEmpty(message.Input.Code)) {
                        throw new ValidationException("package code can't be null or empty");
                    }
                    if (_dicById.ContainsKey(message.Input.GetId())) {
                        return;
                    }
                    KernelData entity = new KernelData().Update(message.Input);
                    _dicById.Add(entity.Id, entity);
                    IRepository<KernelData> repository = NTMinerRoot.CreateServerRepository<KernelData>(isUseJson);
                    repository.Add(entity);

                    VirtualRoot.Happened(new KernelAddedEvent(entity));
                }).AddToCollection(root.ContextHandlers);
            VirtualRoot.Accept<UpdateKernelCommand>(
                "更新内核",
                LogEnum.Console,
                action: message => {
                    InitOnece();
                    if (message == null || message.Input == null || message.Input.GetId() == Guid.Empty) {
                        throw new ArgumentNullException();
                    }
                    if (string.IsNullOrEmpty(message.Input.Code)) {
                        throw new ValidationException("package code can't be null or empty");
                    }
                    if (!_dicById.ContainsKey(message.Input.GetId())) {
                        return;
                    }
                    KernelData entity = _dicById[message.Input.GetId()];
                    if (ReferenceEquals(entity, message.Input)) {
                        return;
                    }
                    entity.Update(message.Input);
                    IRepository<KernelData> repository = NTMinerRoot.CreateServerRepository<KernelData>(isUseJson);
                    repository.Update(entity);

                    VirtualRoot.Happened(new KernelUpdatedEvent(entity));
                }).AddToCollection(root.ContextHandlers);
            VirtualRoot.Accept<RemoveKernelCommand>(
                "移除内核",
                LogEnum.Console,
                action: message => {
                    InitOnece();
                    if (message == null || message.EntityId == Guid.Empty) {
                        throw new ArgumentNullException();
                    }
                    if (!_dicById.ContainsKey(message.EntityId)) {
                        return;
                    }
                    KernelData entity = _dicById[message.EntityId];
                    List<Guid> coinKernelIds = root.CoinKernelSet.Where(a => a.KernelId == entity.Id).Select(a => a.GetId()).ToList();
                    foreach (var coinKernelId in coinKernelIds) {
                        VirtualRoot.Execute(new RemoveCoinKernelCommand(coinKernelId));
                    }
                    _dicById.Remove(entity.Id);
                    IRepository<KernelData> repository = NTMinerRoot.CreateServerRepository<KernelData>(isUseJson);
                    repository.Remove(entity.Id);

                    VirtualRoot.Happened(new KernelRemovedEvent(entity));
                }).AddToCollection(root.ContextHandlers);
        }

        private bool _isInited = false;
        private object _locker = new object();

        public int Count {
            get {
                InitOnece();
                return _dicById.Count;
            }
        }

        private void InitOnece() {
            if (_isInited) {
                return;
            }
            Init();
        }

        private void Init() {
            lock (_locker) {
                if (!_isInited) {
                    IRepository<KernelData> repository = NTMinerRoot.CreateServerRepository<KernelData>(_isUseJson);
                    foreach (var item in repository.GetAll()) {
                        if (!_dicById.ContainsKey(item.GetId())) {
                            _dicById.Add(item.GetId(), item);
                        }
                    }
                    _isInited = true;
                }
            }
        }

        public bool Contains(Guid packageId) {
            InitOnece();
            return _dicById.ContainsKey(packageId);
        }

        public bool TryGetKernel(Guid packageId, out IKernel package) {
            InitOnece();
            KernelData pkg;
            var r = _dicById.TryGetValue(packageId, out pkg);
            package = pkg;
            return r;
        }

        public IEnumerator<IKernel> GetEnumerator() {
            InitOnece();
            return _dicById.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            InitOnece();
            return _dicById.Values.GetEnumerator();
        }
    }
}

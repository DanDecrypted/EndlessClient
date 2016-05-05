﻿// Original Work Copyright (c) Ethan Moffat 2014-2016
// This file is subject to the GPL v2 License
// For additional details, see the LICENSE file

using System;
using System.Threading.Tasks;

namespace EOLib.Net.Communication
{
	public interface ISafeInBandNetworkOperationFactory
	{
		SafeInBandNetworkOperation CreateSafeOperation(Func<Task> networkOperation,
			Action<NoDataSentException> sendErrorAction = null,
			Action<EmptyPacketReceivedException> receiveErrorAction = null);

		SafeInBandNetworkOperation<T> CreateSafeOperation<T>(Func<Task<T>> networkOperation,
			Action<NoDataSentException> sendErrorAction = null,
			Action<EmptyPacketReceivedException> receiveErrorAction = null);
	}
}
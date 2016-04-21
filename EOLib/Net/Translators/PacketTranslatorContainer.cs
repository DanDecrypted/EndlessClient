﻿// Original Work Copyright (c) Ethan Moffat 2014-2016
// This file is subject to the GPL v2 License
// For additional details, see the LICENSE file

using EOLib.Data.Protocol;
using Microsoft.Practices.Unity;

namespace EOLib.Net.Translators
{
	public class PacketTranslatorContainer : IDependencyContainer
	{
		public void RegisterDependencies(IUnityContainer container)
		{
			container.RegisterType<IPacketTranslator<IInitializationData>, InitDataTranslator>();
		}
	}
}

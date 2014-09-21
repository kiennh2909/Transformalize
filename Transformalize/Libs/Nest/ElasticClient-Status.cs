﻿using System;
using System.Threading.Tasks;
using Transformalize.Libs.Elasticsearch.Net.Domain.RequestParameters;
using Transformalize.Libs.Nest.Domain.Responses;
using Transformalize.Libs.Nest.DSL;

namespace Transformalize.Libs.Nest
{
	public partial class ElasticClient
	{
		/// <inheritdoc />
		public IStatusResponse Status(Func<IndicesStatusDescriptor, IndicesStatusDescriptor> selector = null)
		{
			selector = selector ?? (s => s);
			return this.Dispatch<IndicesStatusDescriptor, IndicesStatusRequestParameters, StatusResponse>(
				selector,
				(p, d) => this.RawDispatch.IndicesStatusDispatch<StatusResponse>(p)
			);
		}

		/// <inheritdoc />
		public IStatusResponse Status(IIndicesStatusRequest statusRequest)
		{
			return this.Dispatch<IIndicesStatusRequest, IndicesStatusRequestParameters, StatusResponse>(
				statusRequest,
				(p, d) => this.RawDispatch.IndicesStatusDispatch<StatusResponse>(p)
			);
		}

		/// <inheritdoc />
		public Task<IStatusResponse> StatusAsync(Func<IndicesStatusDescriptor, IndicesStatusDescriptor> selector = null)
		{
			selector = selector ?? (s => s);
			return this.DispatchAsync<IndicesStatusDescriptor, IndicesStatusRequestParameters, StatusResponse, IStatusResponse>(
				selector,
				(p, d) => this.RawDispatch.IndicesStatusDispatchAsync<StatusResponse>(p)
			);
		}

		/// <inheritdoc />
		public Task<IStatusResponse> StatusAsync(IIndicesStatusRequest statusRequest)
		{
			return this.DispatchAsync<IIndicesStatusRequest, IndicesStatusRequestParameters, StatusResponse, IStatusResponse>(
				statusRequest,
				(p, d) => this.RawDispatch.IndicesStatusDispatchAsync<StatusResponse>(p)
			);
		}

	}
}
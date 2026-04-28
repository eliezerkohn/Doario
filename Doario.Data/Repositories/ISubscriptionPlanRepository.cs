using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Doario.Data.Models.SaaS;

namespace Doario.Data.Repositories;

public interface ISubscriptionPlanRepository
{
    Task<List<SubscriptionPlan>> GetAllActiveAsync();
    Task<SubscriptionPlan> GetByIdAsync(Guid planId);
}
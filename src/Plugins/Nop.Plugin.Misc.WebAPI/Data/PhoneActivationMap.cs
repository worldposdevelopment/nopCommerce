using System;
using System.Collections.Generic;
using System.Text;
using FluentMigrator.Builders.Create.Table;
using Nop.Data.Mapping.Builders;
using Nop.Plugin.Misc.WebAPI.Domain;

namespace Nop.Plugin.Misc.WebAPI.Data
{
    class PhoneActivationMap : NopEntityBuilder<PhoneActivation>
    {
        public override void MapEntity(CreateTableExpressionBuilder table)
        {
            table
              .WithColumn(nameof(PhoneActivation.Activated))
              .AsInt32()
              .WithColumn(nameof(PhoneActivation.ActivationCode))
              .AsString(100)
              .WithColumn(nameof(PhoneActivation.DateTime))
              .AsDateTime()
             .WithColumn(nameof(PhoneActivation.Expiry))
              .AsDateTime()
              .WithColumn(nameof(PhoneActivation.PhoneNo))
             .AsString(50);
           
        }
    }
}

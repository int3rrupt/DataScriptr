using AutoMapper;
using DatabaseDevelopment.Models.Schema;

namespace DatabaseDevelopment.Models.Mappings
{
    public class ForeignKeyConstraintProfile : Profile
    {
        public ForeignKeyConstraintProfile()
        {
            IMappingExpression<System.Data.DataRow, ForeignKeyConstraint> mappingExpression;

            mappingExpression = CreateMap<System.Data.DataRow, ForeignKeyConstraint>();
            mappingExpression.ForMember(foreignKeyConstraint => foreignKeyConstraint.ForeignKeyName, cfgExp => cfgExp.MapFrom(dataRow => dataRow["ForeignKeyName"]));
            mappingExpression.ForMember(foreignKeyConstraint => foreignKeyConstraint.TableSchema, cfgExp => cfgExp.MapFrom(dataRow => dataRow["TableSchema"]));
            mappingExpression.ForMember(foreignKeyConstraint => foreignKeyConstraint.TableName, cfgExp => cfgExp.MapFrom(dataRow => dataRow["TableName"]));
            mappingExpression.ForMember(foreignKeyConstraint => foreignKeyConstraint.ConstraintColumnName, cfgExp => cfgExp.MapFrom(dataRow => dataRow["ConstraintColumnName"]));
            mappingExpression.ForMember(foreignKeyConstraint => foreignKeyConstraint.ReferencedTableSchema, cfgExp => cfgExp.MapFrom(dataRow => dataRow["ReferencedTableSchema"]));
            mappingExpression.ForMember(foreignKeyConstraint => foreignKeyConstraint.ReferencedTableName, cfgExp => cfgExp.MapFrom(dataRow => dataRow["ReferencedTableName"]));
            mappingExpression.ForMember(foreignKeyConstraint => foreignKeyConstraint.ReferencedColumnName, cfgExp => cfgExp.MapFrom(dataRow => dataRow["ReferencedColumnName"]));
            mappingExpression.ForMember(foreignKeyConstraint => foreignKeyConstraint.IsDisabled, cfgExp => cfgExp.MapFrom(dataRow => dataRow["IsDisabled"]));
        }
    }
}
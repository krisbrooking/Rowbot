using Rowbot.Entities;
using Rowbot.Framework.Blocks.Connectors.Database;
using Rowbot.UnitTests.Setup;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using Xunit;

namespace Rowbot.UnitTests.Framework.Blocks.Connectors.Database
{
    public class SqlCommandProviderTests
    {
        #region GetQueryCommand

        [Fact]
        public void GetQueryCommand_Should_ReturnQueryCommandWithCommandText()
        {
            var sqlCommandProvider = GetSqlCommandProvider<SourcePerson>();

            var command = sqlCommandProvider.GetQueryCommand();

            Assert.Equal("SELECT [Id],[First_Name],[Last_Name] FROM [SourcePerson]", command.CommandText);
        }
        #endregion

        #region GetFindCommands

        [Fact]
        public void GetFindCommands_Should_CreateSingleDbCommand_WhenFewerParamsThanMaxParameterSize()
        {
            var sqlCommandProvider = GetSqlCommandProvider<SourcePerson>();

            var findEntities = new SourcePerson[] { new SourcePerson() { Id = 1 }, new SourcePerson() { Id = 2 } };
            var commands = sqlCommandProvider.GetFindCommands(findEntities, compare => compare.Include(x => x.Id), result => result.Include(x => x.First_Name)).ToList();

            Assert.Equal("SELECT [Id],[First_Name] FROM [SourcePerson] WHERE ([Id] = @p0Id) OR ([Id] = @p1Id)", commands.Single().CommandText);
        }

        [Fact]
        public void GetFindCommands_Should_CreateDbParameters_WhenFewerParamsThanMaxParameterSize()
        {
            var sqlCommandProvider = GetSqlCommandProvider<SourcePerson>();

            var findEntities = new SourcePerson[] { new SourcePerson() { Id = 1 }, new SourcePerson() { Id = 2 } };
            var commands = sqlCommandProvider.GetFindCommands(findEntities, compare => compare.Include(x => x.Id), result => result.Include(x => x.First_Name)).ToList();

            var parameters = (TestableDataParameterCollection)commands.First().Parameters;

            Assert.Equal("@p0Id", parameters[0].ParameterName);
            Assert.Equal("@p1Id", parameters[1].ParameterName);
        }

        [Fact]
        public void GetFindCommands_Should_CreateSingleDbCommand_ForCompositeCompareWithFewerParamsThanMaxParameterSize()
        {
            var sqlCommandProvider = GetSqlCommandProvider<SourcePerson>();

            var findEntities = new SourcePerson[] { new SourcePerson() { Id = 1, First_Name = "Alice" }, new SourcePerson() { Id = 2, First_Name = "Bob" } };
            var commands = sqlCommandProvider.GetFindCommands(findEntities, compare => compare.Include(x => x.Id).Include(x => x.First_Name), result => result.Include(x => x.Last_Name)).ToList();

            Assert.Equal("SELECT [Id],[First_Name],[Last_Name] FROM [SourcePerson] WHERE ([Id] = @p0Id AND [First_Name] = @p0First_Name) OR ([Id] = @p1Id AND [First_Name] = @p1First_Name)", commands.First().CommandText);
        }

        [Fact]
        public void GetFindCommands_Should_CreateMultipleDbCommands_WhenMoreParamsThanMaxParameterSize()
        {
            var sqlCommandProvider = GetSqlCommandProvider<SourcePerson>();

            var findEntities = Enumerable.Range(0, 11).Select(x => new SourcePerson() { Id = x });
            var commands = sqlCommandProvider.GetFindCommands(findEntities, compare => compare.Include(x => x.Id), result => result.Include(x => x.First_Name), maxParameters: 10);

            Assert.Equal(2, commands.Count());
        }

        [Fact]
        public void GetFindCommands_Should_CreateDbCommandsAndSplitParametersAppropriately_WhenMoreParamsThanMaxParameterSize()
        {
            var sqlCommandProvider = GetSqlCommandProvider<SourcePerson>();

            var findEntities = Enumerable.Range(0, 11).Select(x => new SourcePerson() { Id = x });
            var commands = sqlCommandProvider.GetFindCommands(findEntities, compare => compare.Include(x => x.Id), result => result.Include(x => x.First_Name), maxParameters: 10);

            Assert.Equal(10, commands.First().Parameters.Count);
            Assert.Single(commands.Last().Parameters);
        }

        [Fact]
        public void GetFindCommands_Should_ThrowArgumentException_WhenCompareIsNonMemberExpression()
        {
            var sqlCommandProvider = GetSqlCommandProvider<SourcePerson>();

            Assert.Throws<ArgumentException>(() =>
            {
                var findEntities = new List<SourcePerson> { new SourcePerson() { Id = 1 } };
                sqlCommandProvider.GetFindCommands(findEntities, compare => compare.Include(x => x.Id == 0), result => result.Include(x => x.First_Name)).ToList();
            });
        }

        [Fact]
        public void GetFindCommands_Should_ThrowArgumentException_WhenResultIsNonMemberExpression()
        {
            var sqlCommandProvider = GetSqlCommandProvider<SourcePerson>();

            Assert.Throws<ArgumentException>(() =>
            {
                var findEntities = Enumerable.Range(0, 3).Select(x => new SourcePerson() { Id = x });
                sqlCommandProvider.GetFindCommands(findEntities, compare => compare.Include(x => x.Id), result => result.Include(x => x.First_Name == "BinaryExpression")).ToList();
            });
        }

        [Fact]
        public void GetFindCommands_Should_ReturnEmptyList_WhenLookupValuesCollectionIsEmpty()
        {
            var sqlCommandProvider = GetSqlCommandProvider<SourcePerson>();

            var result = sqlCommandProvider.GetFindCommands(new List<SourcePerson>(), compare => compare.Include(x => x.Id), result => result.Include(x => x.First_Name)).ToList();

            Assert.Empty(result);
        }
        #endregion

        #region GetInsertCommand

        [Fact]
        public void GetInsertCommand_Should_ReturnInsertCommandWithCommandText()
        {
            var sqlCommandProvider = GetSqlCommandProvider<UpdateEntity>();

            var insertCommand = sqlCommandProvider.GetInsertCommand();

            Assert.Equal("INSERT INTO [UpdateEntity] ([Description],[Id],[Name]) VALUES (@Description,@Id,@Name)", insertCommand.CommandText);
        }

        [Fact]
        public void GetInsertCommand_Should_ReturnInsertCommandWithParameters()
        {
            var sqlCommandProvider = GetSqlCommandProvider<UpdateEntity>();

            var insertCommand = sqlCommandProvider.GetInsertCommand();

            Assert.Equal(3, insertCommand.Parameters.Count);
        }
        #endregion

        #region GetUpdateCommand

        [Fact]
        public void GetUpdateCommands_Should_CreateSingleDbCommand_WhenFewerParamsThanMaxParameterSize()
        {
            var sqlCommandProvider = GetSqlCommandProvider<UpdateEntity>();
            var source = GetValidEntities();

            var updateCommands = sqlCommandProvider.GetUpdateCommands(source);

            Assert.Single(updateCommands);
        }

        [Fact]
        public void GetUpdateCommands_Should_ReturnUpdateCommandText_WhenFewerParamsThanMaxParameterSize()
        {
            var sqlCommandProvider = GetSqlCommandProvider<UpdateEntity>();
            var source = GetValidEntities();

            var updateCommands = sqlCommandProvider.GetUpdateCommands(source);

            Assert.Equal("UPDATE [UpdateEntity] SET \r\n[Name] = CASE WHEN [UpdateKey] = @p0UpdateKey THEN @p0Name ELSE [Name] END\r\n, [Description] = CASE WHEN [UpdateKey] = @p1UpdateKey THEN @p1Description ELSE [Description] END\r\nWHERE ([UpdateKey] = @p0UpdateKey) OR ([UpdateKey] = @p1UpdateKey)", updateCommands.First().CommandText);
        }

        [Fact]
        public void GetUpdateCommands_Should_ReturnCommandParameters_WhenFewerParamsThanMaxParameterSize()
        {
            var sqlCommandProvider = GetSqlCommandProvider<UpdateEntity>();
            var source = GetValidEntities();

            var updateCommands = sqlCommandProvider.GetUpdateCommands(source).ToList();

            Assert.Equal("@p0Name:Alice,@p0UpdateKey:1,@p1Description:Desc,@p1UpdateKey:2", GetParametersAsString(updateCommands.First()));
        }

        [Fact]
        public void GetUpdateCommands_Should_CreateMultipleDbCommands_WhenMoreParamsThanMaxParameterSize()
        {
            var sqlCommandProvider = GetSqlCommandProvider<UpdateEntity>();
            var source = GetValidEntities();

            var updateCommands = sqlCommandProvider.GetUpdateCommands(source, maxParameters: 5);

            Assert.Equal(2, updateCommands.Count());
        }

        [Fact]
        public void GetUpdateCommands_Should_CreateMultipleDbCommandsWithCommandText_WhenMoreParamsThanMaxParameterSize()
        {
            var sqlCommandProvider = GetSqlCommandProvider<UpdateEntity>();
            var source = GetValidEntities();

            var updateCommands = sqlCommandProvider.GetUpdateCommands(source, maxParameters: 5).ToList();

            Assert.Equal("UPDATE [UpdateEntity] SET \r\n[Name] = CASE WHEN [UpdateKey] = @p0UpdateKey THEN @p0Name ELSE [Name] END\r\nWHERE ([UpdateKey] = @p0UpdateKey)", updateCommands.First().CommandText);
            Assert.Equal("UPDATE [UpdateEntity] SET \r\n[Description] = CASE WHEN [UpdateKey] = @p0UpdateKey THEN @p0Description ELSE [Description] END\r\nWHERE ([UpdateKey] = @p0UpdateKey)", updateCommands.Last().CommandText);
        }

        [Fact]
        public void GetUpdateCommands_Should_CreateMultipleDbCommandsWithParameters_WhenMoreParamsThanMaxParameterSize()
        {
            var sqlCommandProvider = GetSqlCommandProvider<UpdateEntity>();
            var source = GetValidEntities();

            var updateCommands = sqlCommandProvider.GetUpdateCommands(source, maxParameters: 5).ToList();

            Assert.Equal("@p0Name:Alice,@p0UpdateKey:1", GetParametersAsString(updateCommands.First()));
            Assert.Equal("@p0Description:Desc,@p0UpdateKey:2", GetParametersAsString(updateCommands.Last()));
        }

        [Fact]
        public void GetUpdateCommands_Should_ReturnUpdateCommandText_MultipleChangesCompositeKey()
        {
            var sqlCommandProvider = GetSqlCommandProvider<UpdateCompositeKeyEntity>();
            var source = GetValidCompositeKeyEntities();

            var updateCommands = sqlCommandProvider.GetUpdateCommands(source);

            Assert.Equal("UPDATE [UpdateCompositeKeyEntity] SET \r\n[Name] = CASE WHEN [FirstKey] = @p0FirstKey AND [SecondKey] = @p0SecondKey THEN @p0Name ELSE [Name] END\r\n, [Description] = CASE WHEN [FirstKey] = @p1FirstKey AND [SecondKey] = @p1SecondKey THEN @p1Description ELSE [Description] END\r\nWHERE ([FirstKey] = @p0FirstKey AND [SecondKey] = @p0SecondKey) OR ([FirstKey] = @p1FirstKey AND [SecondKey] = @p1SecondKey)", updateCommands.First().CommandText);
        }

        [Fact]
        public void GetUpdateCommands_Should_ReturnCommandParameters_MultipleChangesCompositeKey()
        {
            var sqlCommandProvider = GetSqlCommandProvider<UpdateCompositeKeyEntity>();
            var source = GetValidCompositeKeyEntities();

            var updateCommands = sqlCommandProvider.GetUpdateCommands(source);

            Assert.Equal("@p0FirstKey:1,@p0Name:Alice,@p0SecondKey:2,@p1Description:Desc,@p1FirstKey:3,@p1SecondKey:4", GetParametersAsString(updateCommands.First()));
        }
        #endregion

        #region CreateDataSetCommand

        [Fact]
        public void GetCreateDataSetCommand_Should_ReturnCommandText_WhenEntityIncludesNoTableConditions()
        {
            var sqlCommandProvider = GetSqlCommandProvider<SourcePerson>();

            var createCommand = sqlCommandProvider.GetCreateDataSetCommand();

            Assert.Equal("CREATE TABLE IF NOT EXISTS [SourcePerson] (\r\n[First_Name] String(300) NULL,\r\n[Id] Int32(0) NOT NULL,\r\n[Last_Name] String(300) NULL\r\n);", createCommand.CommandText);
        }

        [Fact]
        public void GetCreateDataSetCommand_Should_ReturnCommandTextIncludingSchema_WhenEntityIncludesTableAndSchema()
        {
            var sqlCommandProvider = GetSqlCommandProvider<AnnotatedEntity>();

            var createCommand = sqlCommandProvider.GetCreateDataSetCommand();

            Assert.StartsWith("CREATE TABLE IF NOT EXISTS [dbo].[AnnotatedEntities]", createCommand.CommandText);
        }

        [Fact]
        public void GetCreateDataSetCommand_Should_OrderColumnsByIsKeyFirstThenByName_WhenEntityIncludesPrimaryKey()
        {
            var sqlCommandProvider = GetSqlCommandProvider<AnnotatedEntity>();

            var createCommand = sqlCommandProvider.GetCreateDataSetCommand();

            Assert.Equal("CREATE TABLE IF NOT EXISTS [dbo].[AnnotatedEntities] (\r\n[Id] Int32(0) NOT NULL PRIMARY KEY,\r\n[ColumnName] String(300) NOT NULL,\r\n[Description] String(100) NULL\r\n);", createCommand.CommandText);
        }

        [Fact]
        public void GetCreateDataSetCommand_Should_OrderColumnsByColumnOrderThenByIsKeyThenByNameAndAddCompositeKey_WhenEntityIncludesCompositeKey()
        {
            var sqlCommandProvider = GetSqlCommandProvider<UpdateCompositeKeyEntity>();

            var createCommand = sqlCommandProvider.GetCreateDataSetCommand();

            Assert.Equal("CREATE TABLE IF NOT EXISTS [UpdateCompositeKeyEntity] (\r\n[FirstKey] Int32(0) NOT NULL AS IDENTITY,\r\n[SecondKey] Int32(0) NOT NULL AS IDENTITY,\r\n[Description] String(300) NULL,\r\n[Id] Int32(0) NOT NULL,\r\n[Name] String(300) NULL,\r\nPRIMARY KEY([FirstKey],[SecondKey])\r\n);", createCommand.CommandText);
        }

        [Fact]
        public void GetCreateDataSetCommand_Should_GenerateDependentEntity_WhenEntityIncludesForeignKeyOneToOne()
        {
            var sqlCommandProvider = GetSqlCommandProvider<DependentOneToOne>();

            var createCommand = sqlCommandProvider.GetCreateDataSetCommand();

            Assert.Equal("CREATE TABLE IF NOT EXISTS [OneToOne] (\r\n[PrincipalId] Int32(0) NOT NULL PRIMARY KEY,\r\nFOREIGN KEY ([PrincipalId]) REFERENCES [Principal]([PrincipalId])\r\n);", createCommand.CommandText);
        }

        [Fact]
        public void GetCreateDataSetCommand_Should_GenerateDependentEntity_WhenEntityIncludesForeignKeyOneToMany()
        {
            var sqlCommandProvider = GetSqlCommandProvider<DependentOneToMany>();

            var createCommand = sqlCommandProvider.GetCreateDataSetCommand();

            Assert.Equal("CREATE TABLE IF NOT EXISTS [OneToMany] (\r\n[DependentId] Int32(0) NOT NULL PRIMARY KEY,\r\n[PrincipalId] Int32(0) NOT NULL,\r\nFOREIGN KEY ([PrincipalId]) REFERENCES [Principal]([PrincipalId])\r\n);", createCommand.CommandText);
        }

        [Fact]
        public void GetCreateDataSetCommand_Should_GenerateDependentEntity_WhenEntityIncludesForeignKeyManyToMany()
        {
            var sqlCommandProvider = GetSqlCommandProvider<DependentManyToMany>();

            var createCommand = sqlCommandProvider.GetCreateDataSetCommand();

            Assert.Equal("CREATE TABLE IF NOT EXISTS [ManyToMany] (\r\n[PrincipalId] Int32(0) NOT NULL,\r\n[AnnotatedEntityId] Int32(0) NOT NULL,\r\nFOREIGN KEY ([PrincipalId]) REFERENCES [Principal]([PrincipalId]),\r\nFOREIGN KEY ([AnnotatedEntityId]) REFERENCES [dbo].[AnnotatedEntities]([Id]),\r\nPRIMARY KEY([PrincipalId],[AnnotatedEntityId])\r\n);", createCommand.CommandText);
        }
        #endregion

        private SqlCommandProvider<TEntity, TestableDbCommandProvider> GetSqlCommandProvider<TEntity>()
        {
            var entity = new Entity<TEntity>();
            var dbCommandProvider = new TestableDbCommandProvider();
            var sqlCommandProvider = new SqlCommandProvider<TEntity, TestableDbCommandProvider>(dbCommandProvider, entity);
            return sqlCommandProvider;
        }

        private IEnumerable<Update<UpdateEntity>> GetValidEntities()
        {
            return new List<Update<UpdateEntity>>
            {
                new Update<UpdateEntity>(new UpdateEntity(1, 1, "Alice"), new List<FieldDescriptor> { new FieldDescriptor(typeof(UpdateEntity).GetProperty("Name")!) }),
                new Update<UpdateEntity>(new UpdateEntity(2, 2, "Bob"), new List<FieldDescriptor> { new FieldDescriptor(typeof(UpdateEntity).GetProperty("Description")!) })
            };
        }

        private IEnumerable<Update<UpdateCompositeKeyEntity>> GetValidCompositeKeyEntities()
        {
            return new List<Update<UpdateCompositeKeyEntity>>
            {
                new Update<UpdateCompositeKeyEntity>(new UpdateCompositeKeyEntity(1, 2, 1, "Alice"), new List<FieldDescriptor> { new FieldDescriptor(typeof(UpdateEntity).GetProperty("Name")!) }),
                new Update<UpdateCompositeKeyEntity>(new UpdateCompositeKeyEntity(3, 4, 3, "Bob"), new List < FieldDescriptor > { new FieldDescriptor(typeof(UpdateEntity).GetProperty("Description")!) })
            };
        }

        private string GetParametersAsString(IDbCommand command) =>
            string.Join(',',
                command.Parameters
                .Cast<TestableDataParameter>()
                .OrderBy(x => x.ParameterName)
                .Select(x => $"{x.ParameterName}:{x.Value}"));

        public class UpdateEntity
        {
            public UpdateEntity(int updateKey, int id, string name)
            {
                UpdateKey = updateKey;
                Id = id;
                Name = name;
                Description = "Desc";
            }

            [Key]
            [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
            public int UpdateKey { get; set; }
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
        }

        public class UpdateCompositeKeyEntity
        {
            public UpdateCompositeKeyEntity(int firstKey, int secondKey, int id, string name)
            {
                FirstKey = firstKey;
                SecondKey = secondKey;
                Id = id;
                Name = name;
                Description = "Desc";
            }

            [Key]
            [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
            [Column(Order = 1)]
            public int FirstKey { get; set; }

            [Key]
            [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
            [Column(Order = 2)]
            public int SecondKey { get; set; }

            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
        }

        [Table("Principal")]
        public class PrincipalTable
        {
            [Key]
            public int PrincipalId { get; set; }
        }

        [Table("OneToOne")]
        public class DependentOneToOne
        {
            [Key]
            [ForeignKey("PrincipalTable")]
            public int PrincipalId { get; set; }
            public PrincipalTable PrincipalTable { get; set; } = new();
        }

        [Table("OneToMany")]
        public class DependentOneToMany
        {
            [Key]
            public int DependentId { get; set; }

            [ForeignKey("PrincipalTable")]
            public int PrincipalId { get; set; }
            public PrincipalTable PrincipalTable { get; set; } = new();
        }

        [Table("ManyToMany")]
        public class DependentManyToMany
        {
            [Key]
            [Column(Order = 1)]
            [ForeignKey("PrincipalTable")]
            public int PrincipalId { get; set; }
            [Key]
            [Column(Order = 2)]
            [ForeignKey("AnnotatedEntity")]
            public int AnnotatedEntityId { get; set; }
            public PrincipalTable PrincipalTable { get; set; } = new();
            public AnnotatedEntity AnnotatedEntity { get; set; } = new();
        }
    }
}

﻿#region License
//
// Copyright (c) 2008-2013, Dolittle (http://www.dolittle.com)
//
// Licensed under the MIT License (http://opensource.org/licenses/MIT)
//
// You may not use this file except in compliance with the License.
// You may obtain a copy of the license at 
//
//   http://github.com/dolittle/Bifrost/blob/master/MIT-LICENSE.txt
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
#endregion
using System;
using System.Collections.Generic;
using System.Linq;
using Bifrost.Entities;
using Bifrost.Execution;

#if(NETFX_CORE)
using System.Reflection;
#endif

namespace Bifrost.Tasks
{
    /// <summary>
    /// Represents a <see cref="ITaskRepository"/>
    /// </summary>
    public class TaskRepository : ITaskRepository
    {
        IEntityContext<TaskEntity> _entityContext;
        IContainer _container;

        /// <summary>
        /// Initializes a new instance of <see cref="TaskRepository"/>
        /// </summary>
        /// <param name="entityContext"><see cref="IEntityContext{T}"/> that is used for persisting <see cref="Task">tasks</see></param>
        /// <param name="container"><see cref="IContainer"/> to use for creating instances of <see cref="Task">tasks</see></param>
        public TaskRepository(IEntityContext<TaskEntity> entityContext, IContainer container)
        {
            _entityContext = entityContext;
            _container = container;
        }


#pragma warning disable 1591 // Xml Comments
        public IEnumerable<Task> LoadAll()
        {
            return _entityContext.Entities.Select(ToTask);
        }

        public Task Load(TaskId taskId)
        {
            return ToTask(_entityContext.GetById(taskId.Value));
        }

        public void Save(Task task)
        {
            lock (_entityContext)
            {
                var existing = _entityContext.GetById(task.Id.Value);
                if (existing != null)
                    _entityContext.Update(ToTaskEntity(task, existing));
                else
                    _entityContext.Insert(ToTaskEntity(task));

                _entityContext.Commit();
            }
        }

        public void Delete(Task task)
        {
            lock (_entityContext)
            {
                var existing = _entityContext.GetById(task.Id.Value);
                if (existing != null)
                {
                    _entityContext.Delete(existing);
                    _entityContext.Commit();
                }
            }
        }

        public void DeleteById(TaskId taskId)
        {
            lock (_entityContext)
            {
                _entityContext.DeleteById(taskId.Value);
                _entityContext.Commit();
            }
        }
#pragma warning restore 1591 // Xml Comments


        Task ToTask(TaskEntity taskEntity)
        {
            var task = _container.Get(taskEntity.Type) as Task;
            task.Id = taskEntity.Id;
            task.CurrentOperation = taskEntity.CurrentOperation;
            PopulatePropertiesFromState(task, taskEntity);
            return task;
        }

        void PopulatePropertiesFromState(Task target, TaskEntity source)
        {
            var targetType = target.GetType();
            foreach (var key in source.State.Keys)
            {
#if(NETFX_CORE)
                var property = targetType.GetTypeInfo().GetDeclaredProperty(key);
#else
                var property = targetType.GetProperty(key);
#endif
                if (property != null)
                {
                    var value = Convert.ChangeType(source.State[key], property.PropertyType, null);
                    property.SetValue(target, value, null);
                }
            }
        }

        TaskEntity ToTaskEntity(Task task, TaskEntity taskEntity = null)
        {
            if( taskEntity == null ) 
                taskEntity = new TaskEntity();

            taskEntity.Id = task.Id;
            taskEntity.CurrentOperation = task.CurrentOperation;
            taskEntity.Type = task.GetType();
            PopulateStateFromProperties(taskEntity, task);
            return taskEntity;
        }

        void PopulateStateFromProperties(TaskEntity target, Task source)
        {
            var sourceType = source.GetType();
            var taskType = typeof(Task);
#if(NETFX_CORE)
            var taskProperties = taskType.GetTypeInfo().DeclaredProperties;
            var declaringTypeProperties = sourceType.GetTypeInfo().DeclaredProperties.Where(p => p.DeclaringType == sourceType);
#else
            var taskProperties = taskType.GetProperties();
            var declaringTypeProperties = sourceType.GetProperties().Where(p => p.DeclaringType == sourceType);
#endif

            var sourceProperties = declaringTypeProperties.Where(p=>!taskProperties.Any(pp=>pp.Name == p.Name));
            target.State = sourceProperties.ToDictionary(p=>p.Name, p=>p.GetValue(source, null).ToString());
        }
    }
}

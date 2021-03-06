﻿using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Quartz;
using Quartz.Impl;
using Quartz.Simpl;
using Quartz.Impl.Calendar;
using Quartz.Impl.Matchers;
using Quartz.Logging;
using DwFramework.Core;

namespace DwFramework.TaskSchedule
{
    public sealed class TaskScheduleService
    {
        private readonly DirectSchedulerFactory _schedulerFactory;
        private readonly Dictionary<string, IScheduler> _schedulers = new Dictionary<string, IScheduler>();

        public bool EnableLog { get => !LogProvider.IsDisabled; set => LogProvider.IsDisabled = !value; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public TaskScheduleService()
        {
            EnableLog = false;
            _schedulerFactory = DirectSchedulerFactory.Instance;
        }

        /// <summary>
        /// 获取调度器
        /// </summary>
        /// <param name="schedulerKey"></param>
        /// <returns></returns>
        public IScheduler GetScheduler(string schedulerKey)
        {
            if (!_schedulers.ContainsKey(schedulerKey)) throw new Exception("未知调度器");
            return _schedulers[schedulerKey];
        }

        /// <summary>
        /// 创建调度器
        /// </summary>
        /// <param name="schedulerKey"></param>
        /// <returns></returns>
        public async Task CreateSchedulerAsync(string schedulerKey)
        {
            _schedulerFactory.CreateScheduler(schedulerKey, Guid.NewGuid().ToString(), new DefaultThreadPool(), new RAMJobStore());
            var scheduler = await _schedulerFactory.GetScheduler(schedulerKey);
            await scheduler.Start();
            _schedulers[schedulerKey] = scheduler;
        }

        /// <summary>
        /// 添加ScheduleListener
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="schedulerKey"></param>
        public void AddScheduleListener<T>(string schedulerKey) where T : ScheduleListener, new()
        {
            if (!_schedulers.ContainsKey(schedulerKey)) throw new Exception("未知调度器");
            _schedulers[schedulerKey].ListenerManager.AddSchedulerListener(new T());
        }

        /// <summary>
        /// 添加JobListener
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="schedulerKey"></param>
        public void AddJobListener<T>(string schedulerKey) where T : JobListener, new()
        {
            if (!_schedulers.ContainsKey(schedulerKey)) throw new Exception("未知调度器");
            _schedulers[schedulerKey].ListenerManager.AddJobListener(new T(), GroupMatcher<JobKey>.AnyGroup());
        }

        /// <summary>
        /// 添加JobListener
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="schedulerKey"></param>
        /// <param name="name"></param>
        /// <param name="group"></param>
        public void AddJobListener<T>(string schedulerKey, string name, string group) where T : JobListener, new()
        {
            if (!_schedulers.ContainsKey(schedulerKey)) throw new Exception("未知调度器");
            _schedulers[schedulerKey].ListenerManager.AddJobListener(new T(), KeyMatcher<JobKey>.KeyEquals(new JobKey(name, group)));
        }

        /// <summary>
        /// 添加JobListener
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="schedulerKey"></param>
        /// <param name="group"></param>
        public void AddJobListenerForGroup<T>(string schedulerKey, string group) where T : JobListener, new()
        {
            if (!_schedulers.ContainsKey(schedulerKey)) throw new Exception("未知调度器");
            _schedulers[schedulerKey].ListenerManager.AddJobListener(new T(), GroupMatcher<JobKey>.GroupContains(group));
        }

        /// <summary>
        /// 添加JobListener
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="schedulerKey"></param>
        /// <param name="name"></param>
        public void AddJobListenerForName<T>(string schedulerKey, string name) where T : JobListener, new()
        {
            if (!_schedulers.ContainsKey(schedulerKey)) throw new Exception("未知调度器");
            _schedulers[schedulerKey].ListenerManager.AddJobListener(new T(), NameMatcher<JobKey>.NameContains(name));
        }

        /// <summary>
        /// 添加TriggerListener
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="schedulerKey"></param>
        public void AddTriggerListener<T>(string schedulerKey) where T : TriggerListener, new()
        {
            if (!_schedulers.ContainsKey(schedulerKey)) throw new Exception("未知调度器");
            _schedulers[schedulerKey].ListenerManager.AddTriggerListener(new T(), GroupMatcher<TriggerKey>.AnyGroup());
        }

        /// <summary>
        /// 添加TriggerListener
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="schedulerKey"></param>
        /// <param name="name"></param>
        /// <param name="group"></param>
        public void AddTriggerListener<T>(string schedulerKey, string name, string group) where T : TriggerListener, new()
        {
            if (!_schedulers.ContainsKey(schedulerKey)) throw new Exception("未知调度器");
            _schedulers[schedulerKey].ListenerManager.AddTriggerListener(new T(), KeyMatcher<JobKey>.KeyEquals(new TriggerKey(name, group)));
        }

        /// <summary>
        /// 添加TriggerListener
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="schedulerKey"></param>
        /// <param name="group"></param>
        public void AddTriggerListenerForGroup<T>(string schedulerKey, string group) where T : TriggerListener, new()
        {
            if (!_schedulers.ContainsKey(schedulerKey)) throw new Exception("未知调度器");
            _schedulers[schedulerKey].ListenerManager.AddTriggerListener(new T(), GroupMatcher<TriggerKey>.GroupContains(group));
        }

        /// <summary>
        /// 添加TriggerListener
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="schedulerKey"></param>
        /// <param name="name"></param>
        public void AddTriggerListenerForName<T>(string schedulerKey, string name) where T : TriggerListener, new()
        {
            if (!_schedulers.ContainsKey(schedulerKey)) throw new Exception("未知调度器");
            _schedulers[schedulerKey].ListenerManager.AddTriggerListener(new T(), NameMatcher<TriggerKey>.NameContains(name));
        }

        /// <summary>
        /// 释放调度器
        /// </summary>
        /// <param name="schedulerKey"></param>
        /// <returns></returns>
        public Task DisposeSchedulerAsync(string schedulerKey)
        {
            if (!_schedulers.ContainsKey(schedulerKey)) throw new Exception("未知调度器");
            _schedulers.Remove(schedulerKey);
            return _schedulers[schedulerKey].Shutdown(false);
        }

        /// <summary>
        /// 每日排除
        /// </summary>
        /// <param name="schedulerKey"></param>
        /// <param name="calName"></param>
        /// <param name="startHour"></param>
        /// <param name="startMinute"></param>
        /// <param name="startSecond"></param>
        /// <param name="endHour"></param>
        /// <param name="endMinute"></param>
        /// <param name="endSecond"></param>
        /// <returns></returns>
        public Task ExcludeInDayAsync(string schedulerKey, string calName, int startHour = 0, int startMinute = 0, int startSecond = 0, int endHour = 23, int endMinute = 59, int endSecond = 59)
        {
            if (!_schedulers.ContainsKey(schedulerKey)) throw new Exception("未知调度器");
            var calender = new DailyCalendar(DateBuilder.DateOf(startHour, startMinute, startSecond).DateTime, DateBuilder.DateOf(endHour, endMinute, endSecond).DateTime);
            return _schedulers[schedulerKey].AddCalendar(calName, calender, true, true);
        }

        /// <summary>
        /// 每周排除
        /// </summary>
        /// <param name="schedulerKey"></param>
        /// <param name="calName"></param>
        /// <param name="excludeDays"></param>
        /// <returns></returns>
        public Task ExcludeInWeekAsync(string schedulerKey, string calName, DayOfWeek[] excludeDays)
        {
            if (!_schedulers.ContainsKey(schedulerKey)) throw new Exception("未知调度器");
            var calender = new WeeklyCalendar();
            excludeDays.ForEach(item => calender.SetDayExcluded(item, true));
            return _schedulers[schedulerKey].AddCalendar(calName, calender, true, true);
        }

        /// <summary>
        /// 每月排除
        /// </summary>
        /// <param name="schedulerKey"></param>
        /// <param name="calName"></param>
        /// <param name="excludeDays"></param>
        /// <returns></returns>
        public Task ExcludeInMonthAsync(string schedulerKey, string calName, int[] excludeDays)
        {
            if (!_schedulers.ContainsKey(schedulerKey)) throw new Exception("未知调度器");
            var calender = new MonthlyCalendar();
            excludeDays.ForEach(item => calender.SetDayExcluded(item, true));
            return _schedulers[schedulerKey].AddCalendar(calName, calender, true, true);
        }

        /// <summary>
        /// 每年排除
        /// </summary>
        /// <param name="schedulerKey"></param>
        /// <param name="calName"></param>
        /// <param name="excludeMonthDays"></param>
        /// <returns></returns>
        public Task ExcludeInYearAsync(string schedulerKey, string calName, (int, int)[] excludeMonthDays)
        {
            if (!_schedulers.ContainsKey(schedulerKey)) throw new Exception("未知调度器");
            var calender = new AnnualCalendar();
            excludeMonthDays.ForEach(item => calender.SetDayExcluded(DateBuilder.DateOf(0, 0, 0, item.Item2, item.Item1).DateTime, true));
            return _schedulers[schedulerKey].AddCalendar(calName, calender, true, true);
        }

        /// <summary>
        /// 根据Cron表达式排除
        /// </summary>
        /// <param name="schedulerKey"></param>
        /// <param name="calName"></param>
        /// <param name="cronExpression"></param>
        /// <returns></returns>
        public Task ExcludeByCronAsync(string schedulerKey, string calName, string cronExpression)
        {
            if (!_schedulers.ContainsKey(schedulerKey)) throw new Exception("未知调度器");
            var calender = new CronCalendar(cronExpression);
            return _schedulers[schedulerKey].AddCalendar(calName, calender, true, true);
        }

        /// <summary>
        /// 创建任务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="schedulerKey"></param>
        /// <param name="jobBuild"></param>
        /// <param name="triggerBuild"></param>
        /// <param name="jobName"></param>
        /// <param name="jobGroup"></param>
        /// <param name="triggerName"></param>
        /// <param name="triggerGroup"></param>
        /// <returns></returns>
        private Task<DateTimeOffset> CreateJobAsync<T>(string schedulerKey, Func<JobBuilder, JobBuilder> jobBuild, Func<TriggerBuilder, TriggerBuilder> triggerBuild, string jobName = null, string jobGroup = null, string triggerName = null, string triggerGroup = null) where T : IJob
        {
            if (!_schedulers.ContainsKey(schedulerKey)) throw new Exception("未知调度器");
            var jobBuilder = jobBuild(JobBuilder.Create<T>());
            if (jobName != null)
            {
                JobKey key = new JobKey(jobName);
                if (jobGroup != null) key.Group = jobGroup;
                jobBuilder.WithIdentity(key);
            }
            var jobDetails = jobBuilder.Build();
            var triggerBuilder = triggerBuild(TriggerBuilder.Create());
            if (triggerName != null)
            {
                TriggerKey key = new TriggerKey(triggerName);
                if (triggerGroup != null) key.Group = triggerGroup;
                triggerBuilder.WithIdentity(key);
            }
            var trigger = triggerBuilder.Build();
            return _schedulers[schedulerKey].ScheduleJob(jobDetails, trigger);
        }

        /// <summary>
        /// 创建任务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="schedulerKey"></param>
        /// <param name="repeat"></param>
        /// <param name="intervalMilliseconds"></param>
        /// <param name="startAt"></param>
        /// <param name="calName"></param>
        /// <param name="properties"></param>
        /// <param name="jobName"></param>
        /// <param name="jobGroup"></param>
        /// <param name="triggerName"></param>
        /// <param name="triggerGroup"></param>
        /// <returns></returns>
        public Task<DateTimeOffset> CreateJobAsync<T>(string schedulerKey, int repeat, long intervalMilliseconds, DateTimeOffset? startAt = null, string calName = null, IDictionary<string, object> properties = null, string jobName = null, string jobGroup = null, string triggerName = null, string triggerGroup = null) where T : IJob
        {
            if (repeat < 0 || intervalMilliseconds <= 0) throw new Exception("参数错误");
            return CreateJobAsync<T>(schedulerKey, jobBuilder =>
             {
                 if (properties != null) jobBuilder.SetJobData(new JobDataMap(properties));
                 return jobBuilder;
             }, triggerBuilder =>
             {
                 triggerBuilder.WithSimpleSchedule(builder =>
                 {
                     if (repeat == 0) builder.RepeatForever();
                     else builder.WithRepeatCount(repeat);
                     builder.WithInterval(TimeSpan.FromMilliseconds(intervalMilliseconds));
                 });
                 triggerBuilder.StartAt(startAt == null || startAt.Value.Subtract(DateTimeOffset.Now).Ticks < 0 ? DateTimeOffset.Now : startAt.Value);
                 if (calName != null)
                     triggerBuilder.ModifiedByCalendar(calName);
                 return triggerBuilder;
             }, jobName, jobGroup, triggerName, triggerGroup);
        }

        /// <summary>
        /// 创建任务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="schedulerKey"></param>
        /// <param name="cronExpression"></param>
        /// <param name="startAt"></param>
        /// <param name="calName"></param>
        /// <param name="properties"></param>
        /// <param name="jobName"></param>
        /// <param name="jobGroup"></param>
        /// <param name="triggerName"></param>
        /// <param name="triggerGroup"></param>
        /// <returns></returns>
        public Task<DateTimeOffset> CreateJobAsync<T>(string schedulerKey, string cronExpression, DateTimeOffset? startAt = null, string calName = null, IDictionary<string, object> properties = null, string jobName = null, string jobGroup = null, string triggerName = null, string triggerGroup = null) where T : IJob
        {
            return CreateJobAsync<T>(schedulerKey, jobBuilder =>
             {
                 if (properties != null) jobBuilder.SetJobData(new JobDataMap(properties));
                 return jobBuilder;
             }, triggerBuilder =>
             {
                 triggerBuilder.WithCronSchedule(cronExpression);
                 triggerBuilder.StartAt(startAt == null || startAt.Value.Subtract(DateTimeOffset.Now).Ticks < 0 ? DateTimeOffset.Now : startAt.Value);
                 if (calName != null) triggerBuilder.ModifiedByCalendar(calName);
                 return triggerBuilder;
             }, jobName, jobGroup, triggerName, triggerGroup);
        }
    }
}

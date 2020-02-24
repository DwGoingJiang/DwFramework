﻿using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

using Quartz;
using Quartz.Impl;
using Quartz.Simpl;
using Quartz.Impl.Calendar;

using DwFramework.Core;
using DwFramework.Core.Plugins;
using DwFramework.Core.Extensions;
using DwFramework.TaskSchedule.Extensions;

namespace DwFramework.TaskSchedule
{
    public class TaskScheduleService : ITaskScheduleService
    {
        public class Config
        {

        }

        private readonly IRunEnvironment _environment;
        private readonly Config _config;
        private readonly DirectSchedulerFactory _schedulerFactory;

        public IScheduler[] AllSchedulers
        {
            get
            {
                return _schedulerFactory.GetAllSchedulers().Result.ToArray();
            }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="environment"></param>
        public TaskScheduleService(IRunEnvironment environment)
        {
            _environment = environment;
            _config = _environment.GetConfiguration().GetSection<Config>("TaskSchedule");
            _schedulerFactory = DirectSchedulerFactory.Instance;
        }

        /// <summary>
        /// 开启服务
        /// </summary>
        /// <returns></returns>
        public Task OpenServiceAsync()
        {
            return Task.Run(() =>
          {

          });
        }

        /// <summary>
        /// 创建调度器
        /// </summary>
        /// <param name="schedulerKey"></param>
        /// <param name="calendar"></param>
        /// <returns></returns>
        public Task CreateScheduler(string schedulerKey)
        {
            _schedulerFactory.CreateScheduler(schedulerKey, GeneraterPlugin.GenerateRandomString(16), new DefaultThreadPool(), new RAMJobStore());
            var scheduler = _schedulerFactory.GetScheduler(schedulerKey).Result;
            return scheduler.Start();
        }

        /// <summary>
        /// 释放调度器
        /// </summary>
        /// <param name="schedulerKey"></param>
        /// <returns></returns>
        public Task DisposeScheduler(string schedulerKey)
        {
            var scheduler = AllSchedulers.Where(item => item.SchedulerName == schedulerKey).Single();
            if (scheduler == null)
                throw new Exception("未知调度器");
            return scheduler.Shutdown(false);
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
        public Task ExcludeInDay(string schedulerKey, string calName, int startHour = 0, int startMinute = 0, int startSecond = 0, int endHour = 23, int endMinute = 59, int endSecond = 59)
        {
            var scheduler = AllSchedulers.Where(item => item.SchedulerName == schedulerKey).Single();
            if (scheduler == null)
                throw new Exception("未知调度器");
            var calender = new DailyCalendar(DateBuilder.DateOf(startHour, startMinute, startSecond).DateTime, DateBuilder.DateOf(endHour, endMinute, endSecond).DateTime);
            return scheduler.AddCalendar(calName, calender, true, true);
        }

        /// <summary>
        /// 每周排除
        /// </summary>
        /// <param name="schedulerKey"></param>
        /// <param name="calName"></param>
        /// <param name="excludeDays"></param>
        /// <returns></returns>
        public Task ExcludeInWeek(string schedulerKey, string calName, DayOfWeek[] excludeDays)
        {
            var scheduler = AllSchedulers.Where(item => item.SchedulerName == schedulerKey).Single();
            if (scheduler == null)
                throw new Exception("未知调度器");
            var calender = new WeeklyCalendar();
            foreach (var item in excludeDays)
            {
                calender.SetDayExcluded(item, true);
            }
            return scheduler.AddCalendar(calName, calender, true, true);
        }

        /// <summary>
        /// 每月排除
        /// </summary>
        /// <param name="schedulerKey"></param>
        /// <param name="calName"></param>
        /// <param name="excludeDays"></param>
        /// <returns></returns>
        public Task ExcludeInMonth(string schedulerKey, string calName, int[] excludeDays)
        {
            var scheduler = AllSchedulers.Where(item => item.SchedulerName == schedulerKey).Single();
            if (scheduler == null)
                throw new Exception("未知调度器");
            var calender = new MonthlyCalendar();
            foreach (var item in excludeDays)
            {
                calender.SetDayExcluded(item, true);
            }
            return scheduler.AddCalendar(calName, calender, true, true);
        }

        /// <summary>
        /// 每年排除
        /// </summary>
        /// <param name="schedulerKey"></param>
        /// <param name="calName"></param>
        /// <param name="excludeMonthDays"></param>
        /// <returns></returns>
        public Task ExcludeInYear(string schedulerKey, string calName, (int, int)[] excludeMonthDays)
        {
            var scheduler = AllSchedulers.Where(item => item.SchedulerName == schedulerKey).Single();
            if (scheduler == null)
                throw new Exception("未知调度器");
            var calender = new AnnualCalendar();
            foreach (var item in excludeMonthDays)
            {
                calender.SetDayExcluded(DateBuilder.DateOf(0, 0, 0, item.Item2, item.Item1).DateTime, true);
            }
            return scheduler.AddCalendar(calName, calender, true, true);
        }

        /// <summary>
        /// 根据Cron表达式排除
        /// </summary>
        /// <param name="schedulerKey"></param>
        /// <param name="calName"></param>
        /// <param name="cronExpression"></param>
        /// <returns></returns>
        public Task ExcludeByCron(string schedulerKey, string calName, string cronExpression)
        {
            var scheduler = AllSchedulers.Where(item => item.SchedulerName == schedulerKey).Single();
            if (scheduler == null)
                throw new Exception("未知调度器");
            var calender = new CronCalendar(cronExpression);
            return scheduler.AddCalendar(calName, calender, true, true);
        }

        /// <summary>
        /// 创建任务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="jobBuild"></param>
        /// <param name="triggerBuild"></param>
        /// <returns></returns>
        private Task CreateJob<T>(string schedulerKey, Func<JobBuilder, JobBuilder> jobBuild, Func<TriggerBuilder, TriggerBuilder> triggerBuild) where T : ScheduleJob
        {
            var scheduler = AllSchedulers.Where(item => item.SchedulerName == schedulerKey).Single();
            if (scheduler == null)
                throw new Exception("未知调度器");
            var jobDetails = jobBuild(JobBuilder.Create<T>()).Build();
            var trigger = triggerBuild(TriggerBuilder.Create()).Build();
            return scheduler.ScheduleJob(jobDetails, trigger);
        }

        /// <summary>
        /// 创建任务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="repeat"></param>
        /// <param name="intervalMilliseconds"></param>
        /// <returns></returns>
        public Task CreateJob<T>(string schedulerKey, int repeat, long intervalMilliseconds, DateTimeOffset? startAt = null, string calName = null, IDictionary<string, object> properties = null) where T : ScheduleJob
        {
            if (repeat < 0 || intervalMilliseconds <= 0)
                throw new Exception("参数错误");
            return CreateJob<T>(schedulerKey, jobBuilder =>
             {
                 if (properties != null)
                     jobBuilder.SetJobData(new JobDataMap(properties));
                 return jobBuilder;
             }, triggerBuilder =>
             {
                 triggerBuilder.WithSimpleSchedule(builder =>
                 {
                     if (repeat == 0)
                         builder.RepeatForever();
                     else
                         builder.WithRepeatCount(repeat);
                     builder.WithInterval(TimeSpan.FromMilliseconds(intervalMilliseconds));
                 });
                 triggerBuilder.StartAt(startAt == null || startAt.Value.Subtract(DateTimeOffset.Now).Ticks < 0 ? DateTimeOffset.Now : startAt.Value);
                 if (calName != null)
                     triggerBuilder.ModifiedByCalendar(calName);
                 return triggerBuilder;
             });
        }

        /// <summary>
        /// 创建任务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cronExpression"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        public Task CreateJob<T>(string schedulerKey, string cronExpression, DateTimeOffset? startAt = null, string calName = null, IDictionary<string, object> properties = null) where T : ScheduleJob
        {
            return CreateJob<T>(schedulerKey, jobBuilder =>
             {
                 if (properties != null)
                     jobBuilder.SetJobData(new JobDataMap(properties));
                 return jobBuilder;
             }, triggerBuilder =>
             {
                 triggerBuilder.WithCronSchedule(cronExpression);
                 triggerBuilder.StartAt(startAt == null || startAt.Value.Subtract(DateTimeOffset.Now).Ticks < 0 ? DateTimeOffset.Now : startAt.Value);
                 if (calName != null)
                     triggerBuilder.ModifiedByCalendar(calName);
                 return triggerBuilder;
             });
        }
    }
}
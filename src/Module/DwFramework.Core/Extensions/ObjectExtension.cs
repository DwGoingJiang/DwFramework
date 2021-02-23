﻿using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Serialization;
using Mapster;

namespace DwFramework.Core.Extensions
{
    public static class ObjectExtension
    {
        /// <summary>
        /// 类型转换
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static object ConvertTo(this object obj, Type type)
        {
            return obj.Adapt(obj.GetType(), type);
        }

        /// <summary>
        /// 类型转换
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static T ConvertTo<T>(this object obj)
        {
            return obj.Adapt<T>();
        }

        /// <summary>
        /// 序列化
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="type"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static string ToJson(this object obj, Type type, JsonSerializerOptions options = null)
        {
            return JsonSerializer.Serialize(obj, type, options);
        }

        /// <summary>
        /// 序列化
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static string ToJson<T>(this T obj, JsonSerializerOptions options = null)
        {
            return JsonSerializer.Serialize(obj, options);
        }

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="json"></param>
        /// <param name="type"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static object FromJson(this string json, Type type, JsonSerializerOptions options = null)
        {
            return JsonSerializer.Deserialize(json, type, options);
        }

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="json"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static T FromJson<T>(this string json, JsonSerializerOptions options = null)
        {
            return JsonSerializer.Deserialize<T>(json, options);
        }

        /// <summary>
        /// 序列化
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="type"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static string ToXml(this object obj, Type type, Encoding encoding = null)
        {
            encoding ??= Encoding.UTF8;
            using var output = new MemoryStream();
            using var writer = new XmlTextWriter(output, encoding);
            var serializer = new XmlSerializer(type);
            serializer.Serialize(writer, obj);
            return encoding.GetString(output.ToArray());
        }

        /// <summary>
        /// 序列化
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static string ToXml<T>(this T obj, Encoding encoding = null)
        {
            return ToXml(obj, typeof(T), encoding);
        }

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="xml"></param>
        /// <param name="type"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static object FromXml(this string xml, Type type, Encoding encoding = null)
        {
            encoding ??= Encoding.UTF8;
            using var input = new MemoryStream(encoding.GetBytes(xml));
            var serializer = new XmlSerializer(type);
            return serializer.Deserialize(input);
        }

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="xml"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static T FromXml<T>(this string xml, Encoding encoding = null)
        {
            return xml.FromXml(typeof(T), encoding).ConvertTo<T>();
        }

        /// <summary>
        /// 序列化
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static byte[] ToBytes(this object obj, Encoding encoding = null)
        {
            encoding ??= Encoding.UTF8;
            var json = obj.ToJson();
            return encoding.GetBytes(json);
        }

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="type"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static object ToObject(this byte[] bytes, Type type, Encoding encoding = null)
        {
            encoding ??= Encoding.UTF8;
            var json = encoding.GetString(bytes);
            return json.FromJson(type);
        }

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="bytes"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static T ToObject<T>(this byte[] bytes, Encoding encoding = null)
        {
            encoding ??= Encoding.UTF8;
            var json = encoding.GetString(bytes);
            return json.FromJson<T>();
        }
    }
}

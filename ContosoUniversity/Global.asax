



<%@ Application Codebehind="Global.asax.cs" Inherits="ContosoUniversity.MvcApplication" Language="C#" %>
<%-- Shim only: Application_Start delegates to ApplicationStartup.Initialize(), which loads shared settings from AWS Parameter Store and runs one-time init under a DynamoDB distributed lock. --%>

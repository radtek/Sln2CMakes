﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Vs
{
    class VcxProjectParser : VcProjectParser
    {
        private StringBuilder _xml = new StringBuilder();
        protected XmlDocument Document { get; private set; }
        internal VcxProjectParser(string prjName = "", string prjGuid = "") : base(prjName, prjGuid)
        {
            
        }

        protected override bool ParseLine(string lineContent, ref int lineNum)
        {
            _xml.Append(lineContent);
            return base.ParseLine(lineContent, ref lineNum);
        }

        protected override bool PreParsing()
        {
            _xml.Clear();
            Document = null;
            TempProject = null;
            return base.PreParsing();
        }

        protected override void PostParsing()
        {
            Document = new XmlDocument();
            try
            {
                Document.LoadXml(_xml.ToString());
            }
            catch(Exception ex)
            {
                Debug.Print(ex.Message);
                return;
            }

            XmlElement projectElm = Document.DocumentElement;
            VcxProject vcxProj = null;
            if((null!= projectElm) && (string.Compare("Project", projectElm.Name, true) == 0))
            {
                vcxProj = new VcxProject(_prjName);
                vcxProj.AbsolutePath = FileName;
                TempProject = vcxProj;
            }
            else
            {
                return;
            }

            foreach(XmlNode node in projectElm.ChildNodes)
            {
                if (string.Compare("ItemGroup", node.Name, true) == 0)
                {
                    foreach (XmlNode child in node.ChildNodes)
                    {
                        if (string.Compare(child.Name, "ProjectConfiguration", true) == 0)
                        {
                            XmlAttribute nameAtr = null;
                            try
                            {
                                nameAtr = child.Attributes["Include"];
                                if (null != nameAtr)
                                {
                                    VcProjectConfigurationItem prjConfItem = vcxProj.FindConfiguration(nameAtr.Value);
                                    if (null == prjConfItem)
                                    {
                                        prjConfItem = new VcProjectConfigurationItem(nameAtr.Value);
                                        vcxProj.ConfigurationItems.Add(prjConfItem);
                                    }

                                    foreach (XmlNode xmlNode in child.ChildNodes)
                                    {
                                        if (string.Compare(xmlNode.Name, "Configuration", true) == 0)
                                        {
                                            prjConfItem.Configuration = xmlNode.InnerText;
                                        }
                                        else if (string.Compare(xmlNode.Name, "Platform", true) == 0)
                                        {
                                            prjConfItem.Platform = xmlNode.InnerText;
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.Print(ex.Message);
                            }
                        }
                        else if (string.Compare(child.Name, "ClInclude", true) == 0)
                        {
                            XmlAttribute pathAttr = null;
                            try
                            {
                                pathAttr = child.Attributes["Include"];
                                if (null != pathAttr)
                                {
                                    string dirPath = Utilities.Instance.GetFileDirectory(TempProject.AbsolutePath);
                                    string asbFilePath = Path.GetFullPath(dirPath + "\\" + pathAttr.Value);
                                    HeaderFile hdrFile = new HeaderFile(Utilities.Instance.GetFileName(asbFilePath));
                                    hdrFile.AbsolutePath = asbFilePath;
                                    hdrFile.RelativePath = pathAttr.Value;
                                    vcxProj.HeaderFileItems.Add(hdrFile);
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.Print(ex.Message);
                            }
                        }
                        else if (string.Compare(child.Name, "ClCompile", true) == 0)
                        {
                            XmlAttribute pathAttr = null;
                            try
                            {
                                pathAttr = child.Attributes["Include"];
                                if (null != pathAttr)
                                {
                                    string dirPath = Utilities.Instance.GetFileDirectory(TempProject.AbsolutePath);
                                    string asbFilePath = Path.GetFullPath(dirPath + "\\" + pathAttr.Value);
                                    SourceFile srcFile = new SourceFile(Utilities.Instance.GetFileName(asbFilePath));
                                    srcFile.AbsolutePath = asbFilePath;
                                    srcFile.RelativePath = pathAttr.Value;
                                    vcxProj.SourceFileItems.Add(srcFile);
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.Print(ex.Message);
                            }
                        }
                        else if (string.Compare(child.Name, "ResourceCompile", true) == 0)
                        {

                        }
                        else if (string.Compare(child.Name, "Image", true) == 0)
                        {

                        }
                        else if (string.Compare(child.Name, "ProjectReference", true) == 0)
                        {

                        }
                        else if (string.Compare(child.Name, "None", true) == 0)
                        {

                        }
                        else
                        {

                        }
                    }
                }
                else if (string.Compare("PropertyGroup", node.Name, true) == 0)
                {
                    XmlNode labelAttr = null;
                    try
                    {
                        labelAttr = node.Attributes["Label"];
                    }
                    catch (Exception ex)
                    {
                        Debug.Print(ex.Message);
                    }
                    if (null != labelAttr)
                    {
                        if (string.Compare("Globals", labelAttr.Value, true) == 0)
                        {
                            foreach (XmlNode globalNode in node.ChildNodes)
                            {
                                if (string.Compare("ProjectGuid", globalNode.Name, true) == 0)
                                {
                                    vcxProj.Guid = globalNode.InnerText;
                                }
                                else if (string.Compare("Keyword", globalNode.Name, true) == 0)
                                {

                                }
                                else if (string.Compare("RootNamespace", globalNode.Name, true) == 0)
                                {

                                }
                                else if (string.Compare("WindowsTargetPlatformVersion", globalNode.Name, true) == 0)
                                {

                                }
                                else if (string.Compare("ProjectName", globalNode.Name, true) == 0)
                                {
                                    vcxProj.Name = globalNode.InnerText;
                                }
                            }
                        }
                        else if (string.Compare("Configuration", labelAttr.Value, true) == 0)
                        {
                            XmlAttribute condAttr = null;
                            try
                            {
                                condAttr = node.Attributes["Condition"];
                            }
                            catch (Exception ex)
                            {
                                Debug.Print(ex.Message);
                            }
                            if (null != condAttr)
                            {
                                string condVal = condAttr.Value;
                                string[] condParams = condVal.Split('=');
                                if (condParams.Length > 2)
                                {
                                    string condName = Utilities.Instance.UnescapeString(condParams[2], "'");
                                    VcxProjectConfigurationItem configItem = vcxProj.FindConfiguration(condName) as VcxProjectConfigurationItem;
                                    if (null == configItem)
                                    {
                                        configItem = new VcxProjectConfigurationItem(condName);
                                        vcxProj.ConfigurationItems.Add(configItem);
                                    }
                                    VcProjectPropertyGroup propertyGroup = configItem.ProjectPropertyGroup;
                                    if(null== propertyGroup)
                                    {
                                        propertyGroup = new VcProjectPropertyGroup(configItem.Name);
                                        configItem.ProjectPropertyGroup = propertyGroup;
                                    }
                                    foreach (XmlNode propNode in node.ChildNodes)
                                    {
                                        if (string.Compare("ConfigurationType", propNode.Name, true) == 0)
                                        {
                                            if (string.Compare("Application", propNode.Value, true) == 0)
                                            {
                                                propertyGroup.ConfigurationType =  ProjectConfigurationType.Application;
                                            }
                                            else if (string.Compare("StaticLibrary", propNode.Value, true) == 0)
                                            {
                                                propertyGroup.ConfigurationType = ProjectConfigurationType.StaticLibrary;
                                            }
                                            else if (string.Compare("DynamicLibrary", propNode.Value, true) == 0)
                                            {
                                                propertyGroup.ConfigurationType = ProjectConfigurationType.DynamicLibrary;
                                            }
                                            else if (string.Compare("Makefile", propNode.Value, true) == 0)
                                            {
                                                propertyGroup.ConfigurationType = ProjectConfigurationType.Makefile;
                                            }
                                            else
                                            {
                                                propertyGroup.ConfigurationType = ProjectConfigurationType.Unknow;
                                            }
                                        }
                                        else if (string.Compare("OutDir", propNode.Name, true) == 0)
                                        {
                                            propertyGroup.OutDir = propNode.Value;
                                        }
                                        else if (string.Compare("TargetName", propNode.Name, true) == 0)
                                        {
                                            propertyGroup.TargetName = propNode.Value;
                                        }
                                        else if (string.Compare("IntDir", propNode.Name, true) == 0)
                                        {
                                            propertyGroup.IntDir = propNode.Value;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else if (string.Compare("ImportGroup", node.Name, true) == 0)
                {

                }
                else if (string.Compare("ItemDefinitionGroup", node.Name, true) == 0)
                {
                    XmlAttribute condAttr = null;
                    try
                    {
                        condAttr = node.Attributes["Condition"];
                    }
                    catch (Exception ex)
                    {
                        Debug.Print(ex.Message);
                    }
                    if (null != condAttr)
                    {
                        string condName = condAttr.Value;
                        VcxProjectConfigurationItem configItem = vcxProj.FindConfiguration(condName) as VcxProjectConfigurationItem;
                        if(null == configItem)
                        {
                            configItem = new VcxProjectConfigurationItem(condName);
                            vcxProj.ConfigurationItems.Add(configItem);
                        }
                        VcxProjectItemDefinitionGroup itemDefGroup = configItem.ProjectItemDefinitionGroup as VcxProjectItemDefinitionGroup;
                        if(null == itemDefGroup)
                        {
                            itemDefGroup = new VcxProjectItemDefinitionGroup(condName);
                            configItem.ProjectItemDefinitionGroup = itemDefGroup;
                        }
                        foreach (XmlNode itemDefNode in node.ChildNodes)
                        {
                            if (string.Compare("ClCompile", itemDefNode.Name, true) == 0)
                            {
                                VcxProjectCompilationDefintion complDefintion = itemDefGroup.Compilation as VcxProjectCompilationDefintion;
                                if (null == complDefintion)
                                {
                                    complDefintion = new VcxProjectCompilationDefintion();
                                    itemDefGroup.Compilation = complDefintion;
                                }

                                foreach(XmlNode complNode in itemDefNode)
                                {
                                    if (string.Compare("PreprocessorDefinitions", complNode.Name, true) == 0)
                                    {
                                        complDefintion.SetPreprocessors(complNode.InnerText);
                                    }
                                    else if (string.Compare("AdditionalIncludeDirectories", complNode.Name, true) == 0)
                                    {
                                        complDefintion.SetIncludeDirectories(complNode.InnerText);
                                    }
                                }
                            }
                            else if (string.Compare("Link", itemDefNode.Name, true) == 0)
                            {
                                VcxProjectLinkingDefintion linkingDefintion = itemDefGroup.Linking as VcxProjectLinkingDefintion;
                                if(null == linkingDefintion)
                                {
                                    linkingDefintion = new VcxProjectLinkingDefintion();
                                    itemDefGroup.Linking = linkingDefintion;
                                }

                                foreach(XmlNode linkNode in itemDefNode)
                                {
                                    if (string.Compare("AdditionalDependencies", linkNode.Name, true) == 0)
                                    {
                                        linkingDefintion.SetLibraries(linkNode.InnerText);
                                    }
                                    else if (string.Compare("OutputFile", linkNode.Name, true) == 0)
                                    {
                                        linkingDefintion.OutputFile = linkNode.InnerText;
                                    }
                                }
                            }
                        }
                    }
                }
                else if (string.Compare("ProjectExtensions", node.Name, true) == 0)
                {

                }
                else if (string.Compare("Import", node.Name, true) == 0)
                {


                }
                else
                {

                }
            }

            //Completed
            if(null != vcxProj)
            {
                Project = vcxProj;
            }
        }
    }
}

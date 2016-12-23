﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite.Tests
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.MarkdownLite;

    using Xunit;

    [Collection("docfx STA")]
    public class PerfTest
    {
        [Fact]
        [Trait("Related", "Markdown")]
        [Trait("Related", "Perf")]
        public void TestPerf()
        {
            const int RepeatCount = 800;
            string source = GetSource(RepeatCount);
            var builder = new GfmEngineBuilder(new Options());
            var engine = builder.CreateEngine(new HtmlRenderer());
            for (int i = 0; i < 2; i++)
            {
                var result = engine.Markup(source);
                Assert.True(Enumerable.SequenceEqual(GetExpectedLines(RepeatCount), GetLines(result)));
            }
            GC.Collect();
        }

        [Fact]
        [Trait("Related", "Markdown")]
        [Trait("Related", "Perf")]
        public void TestAlice()
        {
            var builder = new GfmEngineBuilder(new Options());
            var engine = builder.CreateEngine(new HtmlRenderer());
            const string source = @"---
title: ""CCheckListBox Class""
ms.custom: na
ms.date: ""10/10/2016""
ms.prod: ""visual-studio-dev14""
ms.reviewer: na
ms.suite: na
ms.technology: 
  - ""devlang-cpp""
ms.tgt_pltfrm: na
ms.topic: ""reference""
f1_keywords: 
  - ""CCheckListBox""
dev_langs: 
  - ""C++""
helpviewer_keywords: 
  - ""CCheckListBox class""
  - ""checklist boxes""
ms.assetid: 1dd78438-00e8-441c-b36f-9c4f9ac0d019
caps.latest.revision: 21
ms.author: ""mblome""
manager: ""ghogen""
translation.priority.ht: 
  - ""cs-cz""
  - ""de-de""
  - ""es-es""
  - ""fr-fr""
  - ""it-it""
  - ""ja-jp""
  - ""ko-kr""
  - ""pl-pl""
  - ""pt-br""
  - ""ru-ru""
  - ""tr-tr""
  - ""zh-cn""
  - ""zh-tw""
---
# CCheckListBox Class
\<?xml version=""1.0"" encoding=""utf-8""?>
\<developerReferenceWithSyntaxDocument xmlns=""http://ddue.schemas.microsoft.com/authoring/2003/5"" xmlns:xlink=""http://www.w3.org/1999/xlink"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:schemaLocation=""http://ddue.schemas.microsoft.com/authoring/2003/5 http://clixdevr3.blob.core.windows.net/ddueschema/developer.xsd"">
    <introduction>
        <para>Provides the functionality of a Windows checklist box. </para>
    </introduction>
    <syntaxSection>
        <legacySyntax>class CCheckListBox : public CListBox</legacySyntax>
    </syntaxSection>
    <section>
        <title>Members</title>
        <content/>
        <sections>
            <section>
                <title>Public Constructors</title>
                <content>
                    \<table xmlns:caps=""http://schemas.microsoft.com/build/caps/2013/11"">
                        <thead>
                            <tr>
                                <TD>
                                    <para>Name</para>
                                </TD>
                                <TD>
                                    <para>Description</para>
                                </TD>
                            </tr>
                        </thead>
                        <tbody>
                            <tr>
                                <TD>
                                    <para> \<link xlink:href=""#cchecklistbox__cchecklistbox"">CCheckListBox::CCheckListBox</link>
                                    </para>
                                </TD>
                                <TD>
                                    <para>Constructs a <unmanagedCodeEntityReference>CCheckListBox</unmanagedCodeEntityReference> object.</para>
                                </TD>
                            </tr>
                        </tbody>
                    </table>
                </content>
            </section>
            <section>
                <title>Public Methods</title>
                <content>
                    \<table xmlns:caps=""http://schemas.microsoft.com/build/caps/2013/11"">
                        <thead>
                            <tr>
                                <TD>
                                    <para>Name</para>
                                </TD>
                                <TD>
                                    <para>Description</para>
                                </TD>
                            </tr>
                        </thead>
                        <tbody>
                            <tr>
                                <TD>
                                    <para> \<link xlink:href=""#cchecklistbox__create"">CCheckListBox::Create</link>
                                    </para>
                                </TD>
                                <TD>
                                    <para>Creates the Windows checklist box and attaches it to the <unmanagedCodeEntityReference>CCheckListBox</unmanagedCodeEntityReference> object.</para>
                                </TD>
                            </tr>
                            <tr>
                                <TD>
                                    <para> \<link xlink:href=""#cchecklistbox__drawitem"">CCheckListBox::DrawItem</link>
                                    </para>
                                </TD>
                                <TD>
                                    <para>Called by the framework when a visual aspect of an owner-draw list box changes.</para>
                                </TD>
                            </tr>
                            <tr>
                                <TD>
                                    <para> \<link xlink:href=""#cchecklistbox__enable"">CCheckListBox::Enable</link>
                                    </para>
                                </TD>
                                <TD>
                                    <para>Enables or disables a checklist box item.</para>
                                </TD>
                            </tr>
                            <tr>
                                <TD>
                                    <para> \<link xlink:href=""#cchecklistbox__getcheck"">CCheckListBox::GetCheck</link>
                                    </para>
                                </TD>
                                <TD>
                                    <para>Gets the state of an item's check box.</para>
                                </TD>
                            </tr>
                            <tr>
                                <TD>
                                    <para> \<link xlink:href=""#cchecklistbox__getcheckstyle"">CCheckListBox::GetCheckStyle</link>
                                    </para>
                                </TD>
                                <TD>
                                    <para>Gets the style of the control's check boxes.</para>
                                </TD>
                            </tr>
                            <tr>
                                <TD>
                                    <para> \<link xlink:href=""#cchecklistbox__isenabled"">CCheckListBox::IsEnabled</link>
                                    </para>
                                </TD>
                                <TD>
                                    <para>Determines whether an item is enabled.</para>
                                </TD>
                            </tr>
                            <tr>
                                <TD>
                                    <para> \<link xlink:href=""#cchecklistbox__measureitem"">CCheckListBox::MeasureItem</link>
                                    </para>
                                </TD>
                                <TD>
                                    <para>Called by the framework when a list box with an owner-draw style is created. </para>
                                </TD>
                            </tr>
                            <tr>
                                <TD>
                                    <para> \<link xlink:href=""#cchecklistbox__ongetcheckposition"">CCheckListBox::OnGetCheckPosition</link>
                                    </para>
                                </TD>
                                <TD>
                                    <para>Called by the framework to get the position of an item's check box.</para>
                                </TD>
                            </tr>
                            <tr>
                                <TD>
                                    <para> \<link xlink:href=""#cchecklistbox__setcheck"">CCheckListBox::SetCheck</link>
                                    </para>
                                </TD>
                                <TD>
                                    <para>Sets the state of an item's check box.</para>
                                </TD>
                            </tr>
                            <tr>
                                <TD>
                                    <para> \<link xlink:href=""#cchecklistbox__setcheckstyle"">CCheckListBox::SetCheckStyle</link>
                                    </para>
                                </TD>
                                <TD>
                                    <para>Sets the style of the control's check boxes.</para>
                                </TD>
                            </tr>
                        </tbody>
                    </table>
                </content>
            </section>
        </sections>
    </section>
    <languageReferenceRemarks>
        <content>
            <para>A ""checklist box"" displays a list of items, such as filenames. Each item in the list has a check box next to it that the user can check or clear. </para>
            <para> <unmanagedCodeEntityReference>CCheckListBox</unmanagedCodeEntityReference> is only for owner-drawn controls because the list contains more than text strings. At its simplest, a checklist box contains text strings and check boxes, but you do not need to have text at all. For example, you could have a list of small bitmaps with a check box next to each item.</para>
            <para>To create your own checklist box, you must derive your own class from <unmanagedCodeEntityReference>CCheckListBox</unmanagedCodeEntityReference>. To derive your own class, write a constructor for the derived class, then call <legacyBold>Create</legacyBold>. </para>
            <para>If you want to handle Windows notification messages sent by a list box to its parent (usually a class derived from \<legacyLink xlink:href=""ca64b77e-2cd2-47e3-8eff-c2645ad578f9"">CDialog</legacyLink>), add a message-map entry and message-handler member function to the parent class for each message.</para>
            <para>Each message-map entry takes the following form:</para>
            <para> <legacyBold>ON_</legacyBold>Notification <legacyBold>(</legacyBold> <parameterReference>id</parameterReference>, <parameterReference>memberFxn</parameterReference> <legacyBold>)</legacyBold>
            </para>
            <para>where <parameterReference>id</parameterReference> specifies the child window ID of the control sending the notification and <parameterReference>memberFxn</parameterReference> is the name of the parent member function you have written to handle the notification.</para>
            <para>The parent's function prototype is as follows:</para>
            <para> <legacyBold>afx_msg</legacyBold> <languageKeyword>void</languageKeyword> <parameterReference>memberFxn</parameterReference> <legacyBold>( );</legacyBold>
            </para>
            <para>There is only one message-map entry that pertains specifically to <legacyBold>CCheckListBox </legacyBold>(but see also the message-map entries for \<legacyLink xlink:href=""7ba3c699-c286-4cd9-9066-532c41ec05d1"">CListBox</legacyLink>):  </para>
            <list class=""bullet"">
                <listItem>
                    <para> <legacyBold>ON_CLBN_CHKCHANGE</legacyBold>   The user has changed the state of an item's checkbox.</para>
                </listItem>
            </list>
            <para>If your checklist box is a default checklist box (a list of strings with the default-sized checkboxes to the left of each), you can use the default \<legacyLink xlink:href=""#cchecklistbox__drawitem"">CCheckListBox::DrawItem</legacyLink> to draw the checklist box. Otherwise, you must override the \<legacyLink xlink:href=""7ba3c699-c286-4cd9-9066-532c41ec05d1#clistbox__compareitem"">CListBox::CompareItem</legacyLink> function and the \<legacyLink xlink:href=""#cchecklistbox__drawitem"">CCheckListBox::DrawItem</legacyLink> and \<legacyLink xlink:href=""#cchecklistbox__measureitem"">CCheckListBox::MeasureItem</legacyLink> functions. </para>
            <para>You can create a checklist box either from a dialog template or directly in your code. </para>
        </content>
    </languageReferenceRemarks>
    <section>
        <title>Inheritance Hierarchy</title>
        <content>
            <para> \<legacyLink xlink:href=""95e9acd3-d9eb-4ac0-b52b-ca4a501a7a3a"">CObject</legacyLink>
            </para>
            <para> \<legacyLink xlink:href=""8883b132-2057-4ce0-a5f2-88979f8f2b13"">CCmdTarget</legacyLink>
            </para>
            <para> \<legacyLink xlink:href=""49a832ee-bc34-4126-88b3-bc1d9974f6c4"">CWnd</legacyLink>
            </para>
            <para> \<legacyLink xlink:href=""7ba3c699-c286-4cd9-9066-532c41ec05d1"">CListBox</legacyLink>
            </para>
            <para> <unmanagedCodeEntityReference>CCheckListBox</unmanagedCodeEntityReference>
            </para>
        </content>
    </section>
    <requirements>
        <content>
            <para> <legacyBold>Header: </legacyBold>afxwin.h</para>
        </content>
    </requirements>
    <section address=""cchecklistbox__cchecklistbox"">
        \<!--7ef3c30a-ddf0-40fc-a9d5-6e7c429d11a0-->
        <title>CCheckListBox::CCheckListBox</title>
        <content>
            <para>Constructs a <unmanagedCodeEntityReference>CCheckListBox</unmanagedCodeEntityReference> object.</para>
            <legacySyntax>CCheckListBox();</legacySyntax>
        </content>
        <sections>
            <section>
                <title>Remarks</title>
                <content>
                    <para>You construct a <unmanagedCodeEntityReference>CCheckListBox</unmanagedCodeEntityReference> object in two steps. First define a class derived from <unmanagedCodeEntityReference>CCheckListBox</unmanagedCodeEntityReference>, then call <legacyBold>Create</legacyBold>, which initializes the Windows checklist box and attaches it to the <unmanagedCodeEntityReference>CCheckListBox</unmanagedCodeEntityReference> object. </para>
                </content>
            </section>
            <section>
                <title>Example</title>
                <content>
                    <codeReference>NVC_MFCControlLadenDialog#60</codeReference>
                </content>
            </section>
        </sections>
    </section>
    <section address=""cchecklistbox__create"">
        \<!--b46bc47a-e7a6-4e2e-ae54-368ec2c0b390-->
        <title>CCheckListBox::Create</title>
        <content>
            <para>Creates the Windows checklist box and attaches it to the <unmanagedCodeEntityReference>CCheckListBox</unmanagedCodeEntityReference> object.</para>
            <legacySyntax>virtual BOOL Create(
    DWORD dwStyle,
    const RECT&amp; rect,
    CWnd* pParentWnd,
    UINT nID );</legacySyntax>
        </content>
        <sections>
            <section>
                <title>Parameters</title>
                <content>
                    <definitionTable>
                        <definedTerm> <parameterReference>dwStyle</parameterReference>
                        </definedTerm>
                        <definition>
                            <para>Specifies the style of the checklist box. The style must be <legacyBold>LBS_HASSTRINGS </legacyBold>and either <legacyBold>LBS_OWNERDRAWFIXED</legacyBold> (all items in the list are the same height) or <legacyBold>LBS_OWNERDRAWVARIABLE</legacyBold> (items in the list are of varying heights). This style can be combined with other \<legacyLink xlink:href=""3f357b8d-9118-4f41-9e28-02ed92d1e88f"">list-box styles</legacyLink> except <legacyBold>LBS_USETABSTOPS</legacyBold>.</para>
                        </definition>
                        <definedTerm> <parameterReference>rect</parameterReference>
                        </definedTerm>
                        <definition>
                            <para>Specifies the checklist-box size and position. Can be either a \<legacyLink xlink:href=""dee4e752-15d6-4db4-b68f-1ad65b2ed6ca"">CRect</legacyLink> object or a RECT structure.</para>
                        </definition>
                        <definedTerm> <parameterReference>pParentWnd</parameterReference>
                        </definedTerm>
                        <definition>
                            <para>Specifies the checklist box's parent window (usually a <unmanagedCodeEntityReference>CDialog</unmanagedCodeEntityReference> object). It must not be <legacyBold>NULL</legacyBold>.</para>
                        </definition>
                        <definedTerm> <parameterReference>nID</parameterReference>
                        </definedTerm>
                        <definition>
                            <para>Specifies the checklist box's control ID.</para>
                        </definition>
                    </definitionTable>
                </content>
            </section>
            <section>
                <title>Return Value</title>
                <content>
                    <para>Nonzero if successful; otherwise 0.</para>
                </content>
            </section>
            <section>
                <title>Remarks</title>
                <content>
                    <para>You construct a <unmanagedCodeEntityReference>CCheckListBox</unmanagedCodeEntityReference> object in two steps. First, define a class derived from <legacyBold>CcheckListBox</legacyBold> and then call <legacyBold>Create</legacyBold>, which initializes the Windows checklist box and attaches it to the <unmanagedCodeEntityReference>CCheckListBox</unmanagedCodeEntityReference>. See \<legacyLink xlink:href=""#cchecklistbox__cchecklistbox"">CCheckListBox::CCheckListBox</legacyLink> for a sample. </para>
                    <para>When <legacyBold>Create</legacyBold> executes, Windows sends the \<legacyLink xlink:href=""49a832ee-bc34-4126-88b3-bc1d9974f6c4#cwnd__onnccreate"">WM_NCCREATE</legacyLink>, \<legacyLink xlink:href=""49a832ee-bc34-4126-88b3-bc1d9974f6c4#cwnd__oncreate"">WM_CREATE</legacyLink>, \<legacyLink xlink:href=""49a832ee-bc34-4126-88b3-bc1d9974f6c4#cwnd__onnccalcsize"">WM_NCCALCSIZE</legacyLink>, and \<legacyLink xlink:href=""49a832ee-bc34-4126-88b3-bc1d9974f6c4#cwnd__ongetminmaxinfo"">WM_GETMINMAXINFO</legacyLink> messages to the checklist-box control. </para>
                    <para>These messages are handled by default by the \<legacyLink xlink:href=""49a832ee-bc34-4126-88b3-bc1d9974f6c4#cwnd__onnccreate"">OnNcCreate</legacyLink>, \<legacyLink xlink:href=""49a832ee-bc34-4126-88b3-bc1d9974f6c4#cwnd__oncreate"">OnCreate</legacyLink>, \<legacyLink xlink:href=""49a832ee-bc34-4126-88b3-bc1d9974f6c4#cwnd__onnccalcsize"">OnNcCalcSize</legacyLink>, and \<legacyLink xlink:href=""49a832ee-bc34-4126-88b3-bc1d9974f6c4#cwnd__ongetminmaxinfo"">OnGetMinMaxInfo</legacyLink> member functions in the <unmanagedCodeEntityReference>CWnd</unmanagedCodeEntityReference> base class. To extend the default message handling, add a message map to the your derived class and override the preceding message-handler member functions. Override <unmanagedCodeEntityReference>OnCreate</unmanagedCodeEntityReference>, for example, to perform needed initialization for a new class.</para>
                    <para>Apply the following \<legacyLink xlink:href=""c85ffbe4-f4ff-4227-917a-48ec4a411842"">window styles</legacyLink> to a checklist-box control:  </para>
                    <list class=""bullet"">
                        <listItem>
                            <para> <legacyBold>WS_CHILD</legacyBold>   Always</para>
                        </listItem>
                        <listItem>
                            <para> <legacyBold>WS_VISIBLE</legacyBold>   Usually</para>
                        </listItem>
                        <listItem>
                            <para> <legacyBold>WS_DISABLED</legacyBold>   Rarely</para>
                        </listItem>
                        <listItem>
                            <para> <legacyBold>WS_VSCROLL</legacyBold>   To add a vertical scroll bar</para>
                        </listItem>
                        <listItem>
                            <para> <legacyBold>WS_HSCROLL</legacyBold>   To add a horizontal scroll bar</para>
                        </listItem>
                        <listItem>
                            <para> <legacyBold>WS_GROUP</legacyBold>   To group controls</para>
                        </listItem>
                        <listItem>
                            <para> <legacyBold>WS_TABSTOP</legacyBold>   To allow tabbing to this control</para>
                        </listItem>
                    </list>
                </content>
            </section>
        </sections>
    </section>
    <section address=""cchecklistbox__drawitem"">
        \<!--ee57edb7-7410-4ecf-9046-22ac3f24d986-->
        <title>CCheckListBox::DrawItem</title>
        <content>
            <para>Called by the framework when a visual aspect of an owner-drawn checklist box changes.</para>
            <legacySyntax>virtual void DrawItem( LPDRAWITEMSTRUCT lpDrawItemStruct );</legacySyntax>
        </content>
        <sections>
            <section>
                <title>Parameters</title>
                <content>
                    <definitionTable>
                        <definedTerm> <parameterReference>lpDrawItemStruct</parameterReference>
                        </definedTerm>
                        <definition>
                            <para>A long pointer to a \<legacyLink xlink:href=""ba9ef1d4-aebb-45e9-b956-4b81a02e50f7"">DRAWITEMSTRUCT</legacyLink> structure that contains information about the type of drawing required.</para>
                        </definition>
                    </definitionTable>
                </content>
            </section>
            <section>
                <title>Remarks</title>
                <content>
                    <para>The <legacyBold>itemAction</legacyBold> and <legacyBold>itemState</legacyBold> members of the <unmanagedCodeEntityReference>DRAWITEMSTRUCT</unmanagedCodeEntityReference> structure define the drawing action that is to be performed. </para>
                    <para>By default, this function draws a default checkbox list, consisting of a list of strings each with a default-sized checkbox to the left. The checkbox list size is the one specified in \<legacyLink xlink:href=""#cchecklistbox__create"">Create</legacyLink>.</para>
                    <para>Override this member function to implement drawing of owner-draw checklist boxes that are not the default, such as checklist boxes with lists that aren't strings, with variable-height items, or with checkboxes that aren't on the left. The application should restore all graphics device interface (GDI) objects selected for the display context supplied in <parameterReference>lpDrawItemStruct</parameterReference> before the termination of this member function.</para>
                    <para>If checklist box items are not all the same height, the checklist box style (specified in <legacyBold>Create</legacyBold>) must be <legacyBold>LBS_OWNERVARIABLE</legacyBold>, and you must override the \<legacyLink xlink:href=""#cchecklistbox__measureitem"">MeasureItem</legacyLink> function.</para>
                </content>
            </section>
        </sections>
    </section>
    <section address=""cchecklistbox__enable"">
        \<!--8a78097b-40f4-4cac-a5bf-0067ecda3454-->
        <title>CCheckListBox::Enable</title>
        <content>
            <para>Call this function to enable or disable a checklist box item.</para>
            <legacySyntax>void Enable(
    int nIndex,
    BOOL bEnabled = TRUE  );</legacySyntax>
        </content>
        <sections>
            <section>
                <title>Parameters</title>
                <content>
                    <definitionTable>
                        <definedTerm> <parameterReference>nIndex</parameterReference>
                        </definedTerm>
                        <definition>
                            <para>Index of the checklist box item to be enabled.</para>
                        </definition>
                        <definedTerm> <parameterReference>bEnabled</parameterReference>
                        </definedTerm>
                        <definition>
                            <para>Specifies whether the item is enabled or disabled.</para>
                        </definition>
                    </definitionTable>
                </content>
            </section>
        </sections>
    </section>
    <section address=""cchecklistbox__getcheck"">
        \<!--dd019fbd-5856-4e50-a108-1710f53f1dab-->
        <title>CCheckListBox::GetCheck</title>
        <content>
            <para>Retrieves the state of the specified check box.</para>
            <legacySyntax>int GetCheck( int nIndex );</legacySyntax>
        </content>
        <sections>
            <section>
                <title>Parameters</title>
                <content>
                    <definitionTable>
                        <definedTerm> <parameterReference>nIndex</parameterReference>
                        </definedTerm>
                        <definition>
                            <para>Zero-based index of a check box that is contained in the list box.</para>
                        </definition>
                    </definitionTable>
                </content>
            </section>
            <section>
                <title>Return Value</title>
                <content>
                    <para>The state of the specified check box. The following table lists possible values.</para>
                    \<table xmlns:caps=""http://schemas.microsoft.com/build/caps/2013/11"">
                        <thead>
                            <tr>
                                <TD>
                                    <para>Value</para>
                                </TD>
                                <TD>
                                    <para>Description</para>
                                </TD>
                            </tr>
                        </thead>
                        <tbody>
                            <tr>
                                <TD>
                                    <para> <unmanagedCodeEntityReference>BST_CHECKED</unmanagedCodeEntityReference>
                                    </para>
                                </TD>
                                <TD>
                                    <para>The check box is checked.</para>
                                </TD>
                            </tr>
                            <tr>
                                <TD>
                                    <para> <unmanagedCodeEntityReference>BST_UNCHECKED</unmanagedCodeEntityReference>
                                    </para>
                                </TD>
                                <TD>
                                    <para>The check box is not checked.</para>
                                </TD>
                            </tr>
                            <tr>
                                <TD>
                                    <para> <unmanagedCodeEntityReference>BST_INDETERMINATE</unmanagedCodeEntityReference>
                                    </para>
                                </TD>
                                <TD>
                                    <para>The check box state is indeterminate.</para>
                                </TD>
                            </tr>
                        </tbody>
                    </table>
                </content>
            </section>
        </sections>
    </section>
    <section address=""cchecklistbox__getcheckstyle"">
        \<!--d4c80914-23d5-4289-b1b5-7095746601c2-->
        <title>CCheckListBox::GetCheckStyle</title>
        <content>
            <para>Call this function to get the checklist box's style.</para>
            <legacySyntax>UINT GetCheckStyle();</legacySyntax>
        </content>
        <sections>
            <section>
                <title>Return Value</title>
                <content>
                    <para>The style of the control's check boxes.</para>
                </content>
            </section>
            <section>
                <title>Remarks</title>
                <content>
                    <para>For information on possible styles, see \<legacyLink xlink:href=""#cchecklistbox__setcheckstyle"">SetCheckStyle</legacyLink>.</para>
                </content>
            </section>
        </sections>
    </section>
    <section address=""cchecklistbox__isenabled"">
        \<!--de8b16f1-78ab-4f22-858e-9ca3126967eb-->
        <title>CCheckListBox::IsEnabled</title>
        <content>
            <para>Call this function to determine whether an item is enabled.</para>
            <legacySyntax>BOOL IsEnabled( int nIndex );</legacySyntax>
        </content>
        <sections>
            <section>
                <title>Parameters</title>
                <content>
                    <definitionTable>
                        <definedTerm> <parameterReference>nIndex</parameterReference>
                        </definedTerm>
                        <definition>
                            <para>Index of the item.</para>
                        </definition>
                    </definitionTable>
                </content>
            </section>
            <section>
                <title>Return Value</title>
                <content>
                    <para>Nonzero if the item is enabled; otherwise 0.</para>
                </content>
            </section>
        </sections>
    </section>
    <section address=""cchecklistbox__measureitem"">
        \<!--c12e8ae3-2170-4c3b-84c7-a97eb0ce7a6f-->
        <title>CCheckListBox::MeasureItem</title>
        <content>
            <para>Called by the framework when a checklist box with a nondefault style is created.</para>
            <legacySyntax>virtual void MeasureItem( LPMEASUREITEMSTRUCT lpMeasureItemStruct );</legacySyntax>
        </content>
        <sections>
            <section>
                <title>Parameters</title>
                <content>
                    <definitionTable>
                        <definedTerm> <parameterReference>lpMeasureItemStruct</parameterReference>
                        </definedTerm>
                        <definition>
                            <para>A long pointer to a \<legacyLink xlink:href=""d141ace4-47cb-46b5-a81c-ad2c5e5a8501"">MEASUREITEMSTRUCT</legacyLink> structure.</para>
                        </definition>
                    </definitionTable>
                </content>
            </section>
            <section>
                <title>Remarks</title>
                <content>
                    <para>By default, this member function does nothing. Override this member function and fill in the <unmanagedCodeEntityReference>MEASUREITEMSTRUCT</unmanagedCodeEntityReference> structure to inform Windows of the dimensions of checklist-box items. If the checklist box is created with the \<legacyLink xlink:href=""3f357b8d-9118-4f41-9e28-02ed92d1e88f"">LBS_OWNERDRAWVARIABLE</legacyLink> style, the framework calls this member function for each item in the list box. Otherwise, this member is called only once.</para>
                </content>
            </section>
        </sections>
    </section>
    <section address=""cchecklistbox__ongetcheckposition"">
        \<!--457281ee-7213-472f-92b3-bd199e041527-->
        <title>CCheckListBox::OnGetCheckPosition</title>
        <content>
            <para>The framework calls this function to get the position and size of the check box in an item.</para>
            <legacySyntax>virtual CRect OnGetCheckPosition(
    CRect rectItem,
    CRect rectCheckBox );</legacySyntax>
        </content>
        <sections>
            <section>
                <title>Parameters</title>
                <content>
                    <definitionTable>
                        <definedTerm>
                            <legacyItalic>rectItem</legacyItalic>
                        </definedTerm>
                        <definition>
                            <para>The position and size of the list item.</para>
                        </definition>
                        <definedTerm> <parameterReference>rectCheckBox</parameterReference>
                        </definedTerm>
                        <definition>
                            <para>The default position and size of an item's check box.</para>
                        </definition>
                    </definitionTable>
                </content>
            </section>
            <section>
                <title>Return Value</title>
                <content>
                    <para>The position and size of an item's check box.</para>
                </content>
            </section>
            <section>
                <title>Remarks</title>
                <content>
                    <para>The default implementation only returns the default position and size of the check box ( <parameterReference>rectCheckBox</parameterReference>). By default, a check box is aligned in the upper-left corner of an item and is the standard check box size. There may be cases where you want the check boxes on the right, or want a larger or smaller check box. In these cases, override <unmanagedCodeEntityReference>OnGetCheckPosition</unmanagedCodeEntityReference> to change the check box position and size within the item. </para>
                </content>
            </section>
        </sections>
    </section>
    <section address=""cchecklistbox__setcheck"">
        \<!--dae358f9-d989-48d7-aec0-18d3c33d9e89-->
        <title>CCheckListBox::SetCheck</title>
        <content>
            <para>Sets the state of the specified check box.</para>
            <legacySyntax>void SetCheck(
    int nIndex,
    int nCheck );</legacySyntax>
        </content>
        <sections>
            <section>
                <title>Parameters</title>
                <content>
                    <definitionTable>
                        <definedTerm> <parameterReference>nIndex</parameterReference>
                        </definedTerm>
                        <definition>
                            <para>Zero-based index of a check box that is contained in the list box.</para>
                        </definition>
                        <definedTerm> <parameterReference>nCheck</parameterReference>
                        </definedTerm>
                        <definition>
                            <para>The button state for the specified check box. See the Remarks section for possible values.</para>
                        </definition>
                    </definitionTable>
                </content>
            </section>
            <section>
                <title>Remarks</title>
                <content>
                    <para>The following table lists possible values for the <parameterReference>nCheck</parameterReference> parameter.</para>
                    \<table xmlns:caps=""http://schemas.microsoft.com/build/caps/2013/11"">
                        <thead>
                            <tr>
                                <TD>
                                    <para>Value</para>
                                </TD>
                                <TD>
                                    <para>Description</para>
                                </TD>
                            </tr>
                        </thead>
                        <tbody>
                            <tr>
                                <TD>
                                    <para>
                                        <system>BST_CHECKED</system>
                                    </para>
                                </TD>
                                <TD>
                                    <para>Select the specified check box.</para>
                                </TD>
                            </tr>
                            <tr>
                                <TD>
                                    <para>
                                        <system>BST_UNCHECKED</system>
                                    </para>
                                </TD>
                                <TD>
                                    <para>Clear the specified check box.</para>
                                </TD>
                            </tr>
                            <tr>
                                <TD>
                                    <para>
                                        <system>BST_INDETERMINATE</system>
                                    </para>
                                </TD>
                                <TD>
                                    <para>Set the specified check box state to indeterminate.</para>
                                    <para>This state is only available if the check box style is <unmanagedCodeEntityReference>BS_AUTO3STATE</unmanagedCodeEntityReference> or <unmanagedCodeEntityReference>BS_3STATE</unmanagedCodeEntityReference>. For more information, see \<link xlink:href=""41206f72-2b92-4250-ae32-31184046402f"">Button Styles</link>.</para>
                                </TD>
                            </tr>
                        </tbody>
                    </table>
                </content>
            </section>
        </sections>
    </section>
    <section address=""cchecklistbox__setcheckstyle"">
        \<!--96c6128e-74cc-45a5-84be-68223f155afe-->
        <title>CCheckListBox::SetCheckStyle</title>
        <content>
            <para>Call this function to set the style of check boxes in the checklist box.</para>
            <legacySyntax>void SetCheckStyle( UINT nStyle );</legacySyntax>
        </content>
        <sections>
            <section>
                <title>Parameters</title>
                <content>
                    <definitionTable>
                        <definedTerm> <parameterReference>nStyle</parameterReference>
                        </definedTerm>
                        <definition>
                            <para>Determines the style of check boxes in the checklist box.</para>
                        </definition>
                    </definitionTable>
                </content>
            </section>
            <section>
                <title>Remarks</title>
                <content>
                    <para>Valid styles are:  </para>
                    <list class=""bullet"">
                        <listItem>
                            <para> <legacyBold>BS_CHECKBOX</legacyBold>
                            </para>
                        </listItem>
                        <listItem>
                            <para> <legacyBold>BS_AUTOCHECKBOX</legacyBold>
                            </para>
                        </listItem>
                        <listItem>
                            <para> <legacyBold>BS_AUTO3STATE</legacyBold>
                            </para>
                        </listItem>
                        <listItem>
                            <para> <legacyBold>BS_3STATE</legacyBold>
                            </para>
                        </listItem>
                    </list>
                    <para>For information on these styles, see \<legacyLink xlink:href=""41206f72-2b92-4250-ae32-31184046402f"">Button Styles</legacyLink>.</para>
                </content>
            </section>
        </sections>
    </section>
    <relatedTopics> \<legacyLink xlink:href=""76798022-5886-48e7-a7f2-f99352b15cbf"">MFC Sample TSTCON</legacyLink> \<link xlink:href=""7ba3c699-c286-4cd9-9066-532c41ec05d1"">Base Class</link> \<link xlink:href=""19d70341-e391-4a72-94c6-35755ce975d4"">Hierarchy Chart</link> \<link xlink:href=""7ba3c699-c286-4cd9-9066-532c41ec05d1"">CListBox</link>
    </relatedTopics>
</developerReferenceWithSyntaxDocument>



";
            var result = engine.Markup(source);
            Assert.True(result.Length > 0);
        }

        [Fact]
        [Trait("Related", "Markdown")]
        [Trait("Related", "Perf")]
        public void TestIntune()
        {
            var builder = new GfmEngineBuilder(new Options());
            var engine = builder.CreateEngine(new HtmlRenderer());
            const string source = @"---
# required metadata

title: Microsoft Intune App SDK for Android Developer Guide | Microsoft Intune
description:
keywords:
author: Msmbaldwin
manager: jeffgilb
ms.date: 09/08/2016
ms.topic: article
ms.prod:
ms.service: microsoft-intune
ms.technology:
ms.assetid: 0100e1b5-5edd-4541-95f1-aec301fb96af

# optional metadata

#ROBOTS:
#audience:
#ms.devlang:
ms.reviewer: jeffgilb
ms.suite: ems
#ms.tgt_pltfrm:
#ms.custom:

---

# Microsoft Intune App SDK for Android Developer Guide

> [!NOTE]
> You may wish to first read the [Intune App SDK overview](intune-app-sdk.md), which covers the current features of the SDK and describes how to prepare for integration on each supported platform. 

## What is in the SDK 

The Intune App SDK for Android is a standard Android library with no external dependencies. 
The SDK composed of:  

* **`Microsoft.Intune MAM.SDK.jar`**: The interfaces necessary to enable MAM in an app, in addition to enabling interoperability with the Microsoft Intune Company Portal app. Apps must specify it as an Android library reference.

* **`Microsoft.Intune.MAM.SDK.Support.v4.jar`**: The interfaces necessary to enable MAM in apps that leverage the android v4 support library.  Apps which need this support must   reference the jar file directly. 

* **`Microsoft.Intune.MAM.SDK.Support.v7.jar`**: The interfaces necessary to enable MAM in apps that leverage the android v7 support library.   Apps which need this support must reference the jar file directly

* **The resource directory**: The resources (such as strings) on which the SDK relies. 

* **`Microsoft.Intune.MAM.SDK.aar`**: The SDK components, with the exception of the Support.V4 and Support.V7 jars. This file can be used in place of the individual components if your build system supports AAR files.

* **`AndroidManifest.xml`**: The additional entry points and the library requirements. 

* **`THIRDPARTYNOTICES.TXT`**:  An attribution notice that acknowledges 3rd party and/or OSS code that will be compiled into your app. 

## Requirements 

The Intune App SDK is a compiled Android project. As a result, it is largely agnostic to the version of Android the app uses for its minimum or target API versions. The SDK supports Android API 14 (Android 4.0+) to Android 24. 

## How the Intune App SDK works 

The Intune App SDK requires changes to an app's source code to enable app management policies. This is done through the replacement of the android base classes with equivalent managed classes, referred to in the document with the prefix `MAM`. The SDK classes live between the Android base class and the app's own derived version of that class.  Using an activity as an example, you end up with an inheritance hierarchy that looks like: `Activity ->MAMActivity->AppSpecificActivity`.

When `AppSpecificActivity` wants to interact with its parent, e.g. `super.onCreate())`, `MAMActivity` is the super class despite being in the inheritance hierarchy and replacing a few methods. Android apps  have a single mode , and have access to the whole system through their Context object.  Apps that have incorporated the Intune App SDK, on the other hand, have dual modes, as the apps continue to access the system through the Context object but, depending on the base Activity used, the Context object will either be provided by Android, or will intelligently multiplex  between a restricted view of the system and the Android-provided Context.

The Intune App SDK for android relies on the presence of the Company Portal app on the device  for enabling MAM policies. When the Company Portal app is not present, the behavior of the MAM enabled app will not be altered, and it will act like any other mobile app. When the Company Portal is installed and has policy for the user, the SDK entry points are initialized asynchronously. Initialization is only required when the process is initially created by Android. During initialization, a connection is established with Company Portal app, and app restriction policy is downloaded.  

## How to integrate with the Intune App SDK
 
As outlined earlier , the SDK requires changes to the app's source code to enable app management policies. Here are the steps necessary to enable MAM in your  app: 

### Replace classes, methods, and activities with their MAM equivalent (Required) 

* Android base classes must be replaced by their MAM equivalent. To do so, find all instances of the classes listed in the table below and replace them with the Intune App SDK equivalent.  

    | Android Class | Intune App SDK Replacement |
    |--|--|
    | android.app.Activity | MAMActivity |
    | android.app.ActivityGroup | MAMActivityGroup |
    | android.app.AliasActivity | MAMAliasActivity |
    | android.app.Application | MAMApplication |
    | android.app.DialogFragment | MAMDialogFragment |
    | android.app.ExpandableListActivity | MAMExpandableListActivity |
    | android.app.Fragment | MAMFragment |
    | android.app.IntentService | MAMIntentService |
    | android.app.LauncherActivity | MAMLauncherActivity |
    | android.app.ListActivity | MAMListActivity |
    | android.app.NativeActivity | MAMNativeActivity |
    | android.app.PendingIntent | MAMPendingIntent |
    | android.app.Service | MAMService |
    | android.app.TabActivity | MAMTabActivity |
    | android.app.TaskStackBuilder | MAMTaskStackBuilder |
    | android.app.backup.BackupAgent | MAMBackupAgent |
    | android.app.backup.BackupAgentHelper | MAMBackupAgentHelper |
    | android.app.backup.FileBackupHelper | MAMFileBackupHelper |
    | android.app.backup.SharePreferencesBackupHelper | MAMSharedPreferencesBackupHelper |
    | android.content.BroadcastReceiver | MAMBroadcastReceiver |
    | android.content.ContentProvider | MAMContentProvider |
    | android.os.Binder | MAMBinder* |
    | android.provider.DocumentsProvider | MAMDocumentsProvider |
    | android.preference.PreferenceActivity | MAMPreferenceActivity |

    *It is only necessary to replace Binder with MAMBinder if the Binder is not generated from an AIDL interface.

    **Microsoft.Intune.MAM.SDK.Support.v4.jar**:

    | Android Class	Intune MAM | SDK Replacement |
    |--|--|
    | android.support.v4.app.DialogFragment | MAMDialogFragment
    | android.support.v4.app.FragmentActivity | MAMFragmentActivity
    | android.support.v4.app.Fragment | MAMFragment
    | android.support.v4.app.TaskStackBuilder | MAMTaskStackBuilder
    | android.support.v4.content.FileProvider | MAMFileProvider
    
    **Microsoft.Intune.MAM.SDK.Support.v7.jar**:

    |Android Class | Intune MAM SDK Replacement |
    |--|--|
    |android.support.v7.app.ActionBarActivity | MAMActionBarActivity |


* When using an android entry point that has been overridden by its MAM equivalent, an alternative version of the entry point's lifecycle must be used (with the exception of the class `MAMApplication`).

    For example, when deriving from `MAMActivity`, instead of overriding `onCreate` and calling `super.onCreate`, the Activity must override `onMAMCreate` and call s`uper.onMAMCreate`. This allows Activity launch (amongst others) to be restricted in certain cases. 

### Enable features that require app participation 

There are some policies the SDK cannot implement on its own. To enable the app to control its behavior for these features, we expose several APIs that you can find in the `AppPolicy` interface included below.  

    /**
     * External facing app policies.
     */
    public interface AppPolicy {
    	/**
    	 * Restrict where an app can save personal data.
    	 * 
    	 * @return True if the app is allowed to save to personal data stores;
    	 *         false otherwise.
    	 */
    	boolean getIsSaveToPersonalAllowed();
    
    	/**
    	 * Check if policy prohibits saving to a content provider location.
    	 * 
    	 * @param location
    	 *            a content URI to check
    	 * @return True if location is not a content URI or if policy does not 
    	 *         prohibit saving to the content location.
    	 */
    	boolean getIsSaveToLocationAllowed(android.net.Uri location); 
    
    	/**
    	 * Whether the SDK PIN prompt is enlightened for the app.
    	 * 
    	 * @return True if the PIN is enabled. False otherwise.
    	 */
    	boolean getIsPinRequired();
    	/**
    	 * Whether the Intune Managed Browser is required to open web links.
    	 *
    	 * @return True if the Managed Browser is required, false otherwise
    	 */
    	boolean getIsManagedBrowserRequired();
    }

### Enable IT admin control over app saving behavior

Many apps implement features that allow the end user to save files locally or to another service. The Intune App SDK allows IT admins to protect against data leakage by applying policy restrictions as they see fit in their organization.  One of the policies that admin can control is if the end user can save to a personal data store. This includes saving to a local location, SD card, or backup services. App participation is needed to enable the feature. If your app allows saving to personal or cloud locations directly from the app, you must implement this feature to ensure that the IT admin can control whether saving to a location is allowed or not. The API below lets the app know whether saving to a personal store is allowed per the current admin policy. The app can then enforce the policy, since it is aware of personal data store available to the end user through the app.  

To determine if the policy is enforced, the app can make the following call: 

    MAMComponents.get(AppPolicy.class).getIsSaveToPersonalAllowed();

**Note**: MAMComponents.get(AppPolicy.class) will always return a non-null App Policy, even if the device or app is not under management. 

### Allow app to detect if PIN Policy is required
 
 There are additional policies where the app may wish to disable some of its functionality so as to not duplicate functionality in the Intune App SDK. For example, if the app has its own PIN user experience, it may wish to disable it if the SDK is configured to require that the end user enter a PIN. 

To determine if PIN policy is configured to require PIN entry periodically, the app can make the following call: 

    MAMComponents.get(AppPolicy.class).getIsPinRequired();

### Registering for notifications from the SDK  

The Intune App SDK allows your app to have control over the behavior when certain policies, such as a remote wipe policy, are used by the IT admin. To do so, you will need to register for notifications from SDK by creating a `MAMNotificationReceiver` class and  registering it with `MAMNotificationReceiverRegistry`. This is done by providing the receiver and the type of notification the receiver wants to receive in  `App.onCreate`, as the example below illustrates:
 
    @Override
	  public void onCreate() {
		    super.onCreate();
    MAMComponents.get(MAMNotificationReceiverRegistry.class).registerReceiver(
    new ToastNotificationReceiver(), MAMNotificationType.WIPE_USER_DATA);
    }

`MAMNotificationReceiver` simply receives notifications. Some notifications are handled by the SDK directly, others require participation of the app. An app must return either true or false from a notification. It must always return true unless some action it tried to take as a result of the notification failed. This failure may be reported to the Intune service, such as if the app indicates it failed to wipe user data. It is safe to block in `MAMNotificationReceiver.onReceive`; its callback is not running on the UI thread. 

The `MAMNotificationReceiver interface is included below as defined in the SDK: 

    /**
     * The SDK is signaling that a MAM event has occurred. 
     * 
     */
    public interface MAMNotificationReceiver {
	  /**
	  * A notification was received.
	  * 
	  * @param notification
	  *            The notification that was received.
    * @return The receiver should return true if it handled the
    *   notification without error (or if it decided to ignore the
  	*   notification). If the receiver tried to take some action in 
    *   response to the notification but failed to complete that
	  *   action it should return false.
	  */
	  boolean onReceive(MAMNotification notification);
    }

The following notifications are sent to the app and some of them may require app participation: 

* **`WIPE_USER_DATA` notification**: This notification is sent in a `MAMUserNotification` class. When this notification is received, the app should delete all data associated with the identity passed with the `MAMUserNotification`. This notification is currently sent during Intune Service un-enrollment. The user's primary name is typically specified during the enrollment process. If you register for this notification, your app must ensure that all user data has been deleted. If you don't register for it, the default selective wipe behavior will be performed. 

* **`WIPE_USER_AUXILIARY_DATA` notification**: Apps can register for this notification if they'd like the Intune App SDK to perform the default wipe, but would still like to remove some auxiliary data when the wipe occurs.  

* **`REFRESH_POLICY` notification**: This notification is sent in a MAMNotification without any additional information. When this notification is received, any cached policy must  be considered no longer invalidated and therefore should check what the policy is. This is generally handled by the SDK, however should be handled by the app if the policy is used in any persistent way. 

### Pending Intents and methods 

After deriving from one of the MAM entry points, you can use the Context as you would normally, for starting Activities, using its `PackageManager`, etc.  `PendingIntents` are an exception to this rule. When calling such classes, you need to change the class name. For example, instead of using `PendingIntent.get*`, `MAMPendingIntents.get*` must be used. 

In some cases, a method available in the Android class has been marked as final in the MAM replacement class. In this case, the MAM replacement class provides a similarly named method (generally suffixed with ""MAM"") which should be overridden instead. For example, instead of overriding `ContentProvider.query`, you would override `MAMContentProvider.queryMAM`. The Java compiler should enforce the final restrictions to prevent accidental override of the original method instead of the MAM equivalent. 

## Protecting Backup data 

As of Android Marshmallow (API 23), Android now has two ways for an app to back up its data. these options are available for use in your app and require different steps to ensure that MAM data protection is applied appropriately. You can review the table below for quick overview on corresponding actions required for correct data protection behavior.  You can also find more on Android backup in the [Android Developer Data Backup guide](http://developer.android.com/guide/topics/data/backup.html). 

### Automatic full backup

In Android M, Android began offering automatic full backups to apps regardless of target API when running on an Android M device. As long as the `android:allowBackup` attribute is not false, an app will receive full, unfiltered backups of their app. This poses a data leak risk, therefore the SDK require the changes outlined in the table below to ensure that data protection is applied.  It is important to follow the guidelines outlined below to protect customer data properly.  If you set `android:allowBackup=false` then your app  will never be queued for backups by the operating system and you have no further actions for MAM, since there will be no backup
 
 ## “key/value” backups

This option is available to all APIs and uses `BackupAgent` and `BackupAgentHelper`. 

#### Using BackupAgentHelper

`BackupAgentHelper` is much simpler to implement than `BackupAgent` both in terms of native Android functionality and MAM integration. `BackupAgentHelper` allows the developer to register entire files and shared preferences to either a `FileBackupHelper` or `SharedPreferencesBackupHelper`, respectively, which are then added to the `BackupAgentHelper` upon creation. 

#### Using BackupAgent

`BackupAgent` allows you to be much more explicit about what data is backed up. However, this options will mean that you will not be able to take advantage of the Android backup framework.  Because you are fairly responsible for the implementation, there are more steps required to ensure appropriate data protection from MAM. Since most of the work is pushed onto you, the developer, MAM integration is slightly more involved. 

##### App does not have a backup agent
  
These are the developer options when `Android:allowbBackup =true`:

###### Full back up according to a configuration file: 

Provide a resource under the `com.microsoft.intune.mam.FullBackupContent` metadata tag in your manifest. e.g.:
    `<meta-data android:name=""com.microsoft.intune.mam.FullBackupContent"" android:resource=""@xml/my_scheme"" />`

Add the following attribute in the `<application>` tag: `android:fullBackupContent=""@xml/my_scheme""`, where `my_scheme` is an XML resource in your app. 

###### Full back dup without exclusions 

Provide a tag in the manifest such as `<meta-data android:name=""com.microsoft.intune.mam.FullBackupContent"" android:value=""true"" />` 
 
Add the following attribute in the `<application>` tag: 
`android:fullBackupContent=""true""`.

##### App has a backup agent

Follow the recommendations in the `BackupAgent` and `BackupAgentHelper` sections as outlined above 

Consider switching to using our `MAMDefaultFullBackupAgent`, which provides easy back up on Android M. 

#### Before your backup

Before beginning your backup, you must check that the files or data buffers you are planning on backing up are indeed allowed to be backed up. We've provided you with an `isBackupAllowed` function in `MAMFileProtectionManager` and `MAMDataProtectionManager` to determine this. If the file or data buffer is not allowed to be backed up then you should not attempt to continue utilizing it in your backup.

At some point during your backup, if you want the identities for the files you checked in step 1 backed up, you must call `backupMAMFileIdentity(BackupDataOutput data, File … files)` with the files you plan to extract data from. This will automatically create new backup entities and write them to the `BackupDataOutput` for you. These entities will be automatically consumed upon restore. 

### Configure Azure Directory Authentication Library (ADAL)  

The SDK relies on ADAL for its authentication and conditional launch scenarios, which requires that apps have some amount of Azure Active Directory configuration. The configuration values are communicated to the SDK via `AndroidManifest` metadata. To configure your app and enable proper authentication, add the following to the app node in the `AndroidManifest`. Some of these configurations are only required if your app uses ADAL for authentication in general; in that case, you will need the specific values that your app use to register itself with AAD. This is done to ensure that the end user does not get prompted for authentication twice due to AAD recognizing two separate registration values: one from the app and one from the SDK. 

        <meta-data
            android:name=""com.microsoft.intune.mam.aad.Authority""
            android:value=""https://AAD authority/"" />
        <meta-data
            android:name=""com.microsoft.intune.mam.aad.ClientID""
            android:value=""your-client-ID-GUID"" />
        <meta-data
            android:name=""com.microsoft.intune.mam.aad.NonBrokerRedirectURI""
            android:value=""your-redirect-URI"" />
        <meta-data
            android:name=""com.microsoft.intune.mam.aad.SkipBroker""
            android:value=""[true | false]"" />

The GUIDs are not expected to have the preceding or trailing curly bracket.

#### Common ADAL configurations 

The following are common configuration for the values explained above. 

##### App does not integrate ADAL

* The authority must  be set to the desired environment where AAD accounts have been configured.

* SkipBroker must be set to true.

##### App integrates ADAL

* The authority must be set to the desired environment where AAD accounts have been configured.

* Client ID must be set to the app's client ID.

* `NonBrokerRedirectURI` should be set to a valid redirect URI for the app.
    * Or `urn:ietf:wg:oauth:2.0:oob` must be configured as a valid AAD redirect URI.

* SkipBroker must be set to false (or be absent)

* AAD must be configured to accept the broker redirect URI.

##### App integrates ADAL but does not support the AAD Authenticator app.

* The authority must be set to the desired environment where AAD accounts have been configured.

* Client ID must be set to the app's client ID.

* `NonBrokerRedirectURI` must be set to a valid redirect URI for the app.

    * Or `urn:ietf:wg:oauth:2.0:oob` should be configured as a valid AAD redirect URI.

### How to enable logging in the SDK 

Logging is done through the `java.util.logging` framework. To receive the logs, set up global logging as described in the [Java technical guide](http://docs.oracle.com/javase/6/docs/technotes/guides/logging/overview.html). Depending on the app, `App.onCreate` is usually the best place  to initiate logging. Please note that Log messages are keyed by the class name, which may be obfuscated.

## Known Platform Limitations 

### File Size Limitations 

On Android, the limitations of the Dalvik executable file format may become an issue for large code bases that run without ProGuard. Specifically, the following limitations may  occur: 

* The 65K limit on fields.

* The 65K limit on methods.

When including many projects, every android:package will get a copy of R  which will dramatically increase the number of fields as libraries are added. The following recommendations may help mitigate this limitation:
 
* All library projects should share the same android:package where possible. This will not sporadically fail in the field, since it is purely a build-time problem.   In addition, newer versions of the Android SDK will pre-process DEX files to remove some of the redundancy. This lowers the distance from the fields even further.

* Use the newest Android SDK build tools available.

* Remove all unnecessary and unused libraries (e.g. `android.support.v4`)

### Policy Enforcement Limitations

**Screen Capture**: The SDK is unable to enforce a new screen capture setting value in Activities that have already gone through Activity.onCreate. This can result in a period of time where the app has been configured to disable screenshots but screenshots can still be taken.

**Using Content Resolvers*: The transfer or receive policy may block or partially block the use of a content resolver to access the content provider in another app. This will cause ContentResolver methods to return null or throw a failure value (e.g. `openOutputStream` will throw `FileNotFoundException` if blocked). The app can determine whether a failure to write data through a content resolver was caused by policy (or would be caused by policy) by making the call:

    MAMComponents.get(AppPolicy.class).getIsSaveToLocationAllowed(contentURI)

**Exported Services**: The `AndroidManifest.xml` file included in the Intune App SDK contains `MAMNotificationReceiverService`, which must be an exported service to allow the Company Portal to send notifications to an enlightened app. The service checks the caller to ensure that only the Company Portal is allowed to send notifications. 

## Recommended Android Best Practices 

The Intune SDK maintains the contract provided by the Android API, though failure conditions may be triggered more frequently as a result of policy enforcement. These Android best practices will reduce the likelihood of failure: 

* Android SDK Functions that may return null have a higher likelihood of being null now.  To minimize issues, ensure that null checks are in the right places.

* Features that can be checked for must be checked for through their SDK APIs.

* Any derived functions must call through to their super class versions.

* Avoid use of any API in an ambiguous way. For example, `Activity.startActivityForResult/onActivityResult` without checking the requestCode will cause strange behavior.";
            var result = engine.Markup(source);
            Assert.True(result.Length > 0);
        }

        private static string GetSource(int RepeatCount)
        {
            const string source = @"
Heading
=======
 
Sub-heading
-----------
  
### Another deeper heading
  
Paragraphs are separated
by a blank line.
 
Leave 2 spaces at the end of a line to do a  
line break
 
Text attributes *italic*, **bold**, 
`monospace`, ~~strikethrough~~ .
 
A [link](http://example.com).

Shopping list:
 
* apples
* oranges
* pears
 
Numbered list:
 
1. apples
2. oranges
3. pears
";
            return string.Concat(Enumerable.Repeat(source, RepeatCount));
        }

        private static IEnumerable<string> GetLines(string text)
        {
            var sr = new StringReader(text);
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                yield return line;
            }
        }

        private static IEnumerable<string> GetExpectedLines(int count)
        {
            for (int i = 0; i < count; i++)
            {
                string idPostFix;
                if (i == 0)
                {
                    idPostFix = string.Empty;
                }
                else
                {
                    idPostFix = "-" + i.ToString();
                }
                yield return $@"<h1 id=""heading{idPostFix}"">Heading</h1>";
                yield return $@"<h2 id=""sub-heading{idPostFix}"">Sub-heading</h2>";
                yield return $@"<h3 id=""another-deeper-heading{idPostFix}"">Another deeper heading</h3>";
                yield return @"<p>Paragraphs are separated";
                yield return @"by a blank line.</p>";
                yield return @"<p>Leave 2 spaces at the end of a line to do a<br>line break</p>";
                yield return @"<p>Text attributes <em>italic</em>, <strong>bold</strong>, ";
                yield return @"<code>monospace</code>, <del>strikethrough</del> .</p>";
                yield return @"<p>A <a href=""http://example.com"" data-raw-source=""[link](http://example.com)"">link</a>.</p>";
                yield return @"<p>Shopping list:</p>";
                yield return @"<ul>";
                yield return @"<li>apples</li>";
                yield return @"<li>oranges</li>";
                yield return @"<li>pears</li>";
                yield return @"</ul>";
                yield return @"<p>Numbered list:</p>";
                yield return @"<ol>";
                yield return @"<li>apples</li>";
                yield return @"<li>oranges</li>";
                yield return @"<li>pears</li>";
                yield return @"</ol>";
            }
        }
    }
}

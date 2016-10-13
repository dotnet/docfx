// Copyright (c) Microsoft. All rights reserved.
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
            const int RepeatCount = 1000;
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
                yield return @"<p>A <a href=""http://example.com"">link</a>.</p>";
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

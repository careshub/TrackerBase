<%@ Page Language="VB" MasterPageFile="~/Site.master" AutoEventWireup="false" CodeFile="ProjectMgmt.aspx.vb" Inherits="ProjectMgmt" %>
<%@ MasterType virtualpath="~/Site.master" %>

<asp:Content ID="HeaderContent" ContentPlaceHolderID="HeadContent" Runat="Server">
  <link href="/Styles/ProjectMgmt.css?v=20150918" rel="stylesheet" type="text/css" />
  <script type="text/javascript" id="uxPageJS"> var projectsJson = {};var states = "";</script>
  <asp:literal ID="paramHolder" runat="server" />
  <script type="text/javascript" src="/Scripts/ProjectMgmt.js?v=20150918"></script>
</asp:Content>
<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" Runat="Server">

<div class="" >
  <p><asp:Label runat="server" ID="uxOzzyInfo"></asp:Label></p>
  <p><asp:Label runat="server" ID="uxInfo"></asp:Label></p>
</div>

<div class="col--mask right--menu clear">
<%-- <div class="col--left">--%>
  <div class="col--2 mgmt-menu">
    <div id="uxAccordionProjectTools" class="accordion">
      <h3 id="uxTest">Actions</h3>
      <div>
        <p>
        <input type="button" id="uxCreateNewProject" value="Create New Project" title="Create a new project" class="main-menu" onclick="CreateNewProject();" />  
        </p>
        <p>Select a project to use the following:</p>
        <p>
        <asp:Button ID="uxOpenProject" runat="server" Text="Open" CssClass="main-menu" ToolTip="Open the selected project" OnClientClick="return CanOpenProject();" />
        <asp:Button ID="uxEditProject" runat="server" Text="Edit" CssClass="main-menu" ToolTip="Edit the selected project" OnClientClick="EditProject(); return false;" />
        <asp:Button ID="uxDeleteProject" runat="server" Text="Delete" CssClass="main-menu" ToolTip="Delete the selected project" OnClientClick="return CanDeleteProject();" />
        <input type="button" id="uxExportProject" value="Export" title="Export project files" class="main-menu" onclick="SetExportProject();" />
        <%--<input type="button" id="uxImportProject" value="Import" title="Import project files" class="main-menu" onclick="SetImportProject();" />--%>
        <input type="button" id="uxDuplicateProject" value="Duplicate" title="Duplicate the selected project" class="main-menu" onclick="DuplicateProject();" />
        <input type="button" id="uxTransferProject" value="Transfer" title="Transfer a copy of the selected project to another user" class="main-menu" onclick="TransferProject();" />
        </p> 
      </div>
    </div>
  </div>
  <div class="col--1 mgmt-main">
    <asp:Label ID="uxNoProjects" Visible="true" runat="server" ForeColor="Red" Font-Size="Medium" Text="" ></asp:Label>
  
    <div id="uxProjectsContainer"><%-- to be rendered with templating --%>
    </div>

<script id="projectsTmpl" type="text/x-jsrender">
{^{for projects}}
<div id="uxProjectAccordion{{:#index}}" class="accord-group notaccordion collapsible collapsed">
  <h3 id="uxProjectsHeader{{:#index}}" class="accord-header-items">
    <input type=hidden id="uxProjectProjectId{{:#index}}" value="{{:Project.ObjectID}}" />
    <input type=hidden id="uxProjectFolder{{:#index}}" value="{{:Project.Folder}}" />
    <input type=hidden id="uxProjectOwnerGuid{{:#index}}" value="{{:Project.OwnerGuid}}" />
    <input type=hidden id="uxProjectCreatorGuid{{:#index}}" value="{{:Project.CreatorGuid}}" />
    <input type=hidden id="uxProjectEditorGuid{{:#index}}" value="{{:Project.EditorGuid}}" />
    <input type=hidden id="uxOperationId{{:#index}}" value="{{:Operation.ObjectID}}" />
    <input type=hidden id="uxOperationGuid{{:#index}}" value="{{:OperationDatum.GUID}}" />
    <input type=hidden id="uxProjectRoleId{{:#index}}" value="{{:Role.RoleID}}" />
    <input type="radio" name="ProjectSelect" id="uxProjectSelect{{:#index}}" class="accord-sel" />
    <span class="accord-header-separate"><span>Project Name: </span>
      <span class="text-left" id="uxProjectName{{:#index}}">{{:Project.Name}}</span>
        <%--<span class="display-none">Owner: </span><span class="text-left">{{:Project.Owner}}</span>--%>
    </span>
  </h3>
  <div>
    <table id="uxSelectedProjectDetails" class="project-table full-width">
    <tbody>
    <tr>
      <td>Created: </td><td><label id="uxProjectCreatedOn{{:#index}}">{{SDate Project.Created /}}</label></td>
      <td class="display-none">Edited: </td><td class="display-none"><label id="uxProjectEditedOn{{:#index}}" >{{SDate Project.Edited /}}</label></td>
    </tr>
    <tr class="display-none">
      <td>Security Role: </td>
      <td><label id="uxProjectSecurityRole">{{:Role.RoleName}}</label></td>
      <td>Assigned by: </td>
      <td><label id="uxProjectRoleCreator">{{:Role.Creator}}</label></td>
    </tr>
    <tr>
      <td>Operation Name: </td>
      <td><label id="uxProjectOperationName">{{:Operation.OperationName}}</label></td>
      <td>Project Start: </td>
      <td><label id="uxProjectStartMonth{{:#index}}">{{CalMonth Operation.StartCalMonth /}}</label>, <label id="uxProjectStartYear{{:#index}}">{{:Operation.StartCalYear}}</label></td>
    </tr>
    <tr>
      <td>Contact: </td>
      <td><label id="uxProjectContact">{{:Operation.Contact}}</label></td>
      <td>Address: </td>
      <td rowspan="2"><label id="uxProjectAddress">{{:Operation.Address}}</label></td>
    </tr>
    <tr>
      <td>Office Phone: </td>
      <td><label id="uxProjectContactOfficePhone">{{:Operation.ContactOfficePhone}}</label></td>
    </tr>
    <tr>
      <td>Home Phone: </td>
      <td><label id="uxProjectContactHomePhone">{{:Operation.ContactHomePhone}}</label></td>
      <td>&nbsp;</td>
      <td rowspan="2"><label id="uxProjectCity">{{:Operation.City}}</label>, <label id="uxProjectState">{{:Operation.State}}</label>&nbsp;
          <label id="uxProjectZip">{{:Operation.Zip}}</label></td>
    </tr>
    <tr>
      <td>Email: </td>
      <td><label id="uxProjectContactEmail">{{:Operation.ContactEmail}}</label></td>
    </tr>
    <tr>
      <td>Notes: </td>
      <td rowspan="6" class="notes"><label id="uxProjectNotes">{{:OperationDatum.Notes}}</label></td>
      <td>County: </td>
      <td><label id="uxProjectCountyName">{{:Operation.CountyName}}</label></td>
    </tr>
    </tbody>
    </table>
  </div>
</div>
{{/for}}

<div id="uxEditProjectContainer" class="display-none popup-container" >
  <div id="uxEditProjectBackground" class="popup-background"></div>
  <div id="uxEditProjectForm" class="popup-form draggable">
    <div id="uxEditProjectHeader" class="popup-header">
      <h3>Edit project</h3>
      <div class="popup-control-panel">
        <img title="This form is draggable when you see the arrows" alt="Draggable form" src="/images/move.png" 
              class="control-drag" />
        <img title="Close this form" alt="Close form" src="/images/close.png"
              id="uxEditProjectClose" class="control-close" onclick="CloseForm('uxEditProject');" />
      </div>
      <hr id="uxEditProjectDragLineUpper" />
      <p><label id="uxEditProjectInfo" class="">Please enter the following information. Some fields are required.</label></p>
    </div>
    <div id="uxEditProjectMain">
      <p><label id="uxEditProjectNameInfo" class="left-column">Project Name (required):</label>
      <input data-link="{:selected().Project.Name}" id="uxEditProjectName" type="text" name="text" maxlength="50" 
        onchange="TrimStart(this);ValidateProjectName(this);ImposeMaxLength(this, 50);" onblur="TrimInput(this);this.onchange();" onkeyup="this.onchange();"
        onkeypress="this.onchange();" class="right-column" />
      </p>
      <p>
        <span class="text-small left-column"><span id="uxEditProjectNameCount">50</span><span> characters remaining</span></span>
        <label id="uxEditProjectNameDesc" class="text-small right-column">Name must begin with a letter and may contain letters, numbers, dashes and underscores.</label>
      </p>
      <p>
      <label id="uxEditProjectStartingYear" class="left-column">Start Year:</label>
      <select data-link="{:selected().Operation.StartCalYear} disabled{:selectedID === '0'}" id="uxEditProjectStartCalYear" name="number" class="right-column">
      </select>
      </p>
      <p>
      <label id="uxEditProjectStartingMonth" class="left-column">Start Month:</label>
      <select data-link="{:selected().Operation.StartCalMonth} disabled{:selectedID === '0'}" id="uxEditProjectStartCalMonth" name="number" class="right-column">
        <option value="1">January</option>
        <option value="2">February</option>
        <option value="3">March</option>
        <option value="4">April</option>
        <option value="5">May</option>
        <option value="6">June</option>
        <option value="7">July</option>
        <option value="8">August</option>
        <option value="9">September</option>
        <option value="10">October</option>
        <option value="11">November</option>
        <option value="12">December</option>
      </select>
      </p>
      <p><label id="uxEditProjectOperationNameInfo" class="left-column">Operation:</label>
      <input data-link="{:selected().Operation.OperationName} disabled{:selectedID === '0'}" id="uxEditProjectOperationName" type="text" name="text" maxlength="30" onblur="TrimInput(this);" class="right-column" />
      </p>
      <p><label id="uxEditProjectAddressInfo" class="left-column">Address:</label>
      <input data-link="{:selected().Operation.Address} disabled{:selectedID === '0'}" id="uxEditProjectAddress" type="text" name="text" maxlength="30" onblur="TrimInput(this);" class="right-column" />
      </p><p>
      <label id="uxEditProjectCityInfo" class="left-column">City:</label>
      <input data-link="{:selected().Operation.City} disabled{:selectedID === '0'}" id="uxEditProjectCity" type="text" name="text" maxlength="20" onblur="TrimInput(this);" class="right-column" />
      </p>
      <p>
      <label id="uxEditProjectZipInfo" class="left-column">Zip:</label>
      <input data-link="{:selected().Operation.Zip} disabled{:selectedID === '0'}" id="uxEditProjectZip" type="text" name="text" maxlength="10" onblur="TrimInput(this);" class="right-column" />
      </p>
      <p>
      <label id="uxEditProjectContactInfo" class="left-column">Contact:</label>
      <input data-link="{:selected().Operation.Contact} disabled{:selectedID === '0'}" id="uxEditProjectContact" type="text" name="text" maxlength="30" onblur="TrimInput(this);" class="right-column" />
      </p>
      <p>
      <label id="uxEditProjectOfficePhoneInfo" class="left-column">Office Phone:</label>
      <input data-link="{:selected().Operation.ContactOfficePhone} disabled{:selectedID === '0'}" id="uxEditProjectContactOfficePhone" type="text" name="text" maxlength="14" onblur="TrimInput(this);" class="right-column" />
      </p>
      <p>
      <label id="uxEditProjectHomePhoneInfo" class="left-column">Home Phone:</label>
      <input data-link="{:selected().Operation.ContactHomePhone} disabled{:selectedID === '0'}" id="uxEditProjectContactHomePhone" type="text" name="text" maxlength="14" onblur="TrimInput(this);" class="right-column" />
      </p>
      <p>
      <label id="uxEditProjectEmailInfo" class="left-column">Email:</label>
      <input data-link="{:selected().Operation.ContactEmail} disabled{:selectedID === '0'}" id="uxEditProjectContactEmail" type="text" name="text" maxlength="40" onblur="TrimInput(this);" class="right-column" />
      </p>
      <p>
      <label id="uxEditOperationNotesInfo" class="left-column">Notes:
        <br /><span class="text-small"><span id="uxEditProjectNotesCount">100</span><span> characters remaining</span></span></label>
      <textarea data-link="{:selected().OperationDatum.Notes} disabled{:selectedID === '0'}" id="uxEditProjectNotes" 
        name="text" class="right-column" cols="34" rows="3" 
        onchange="TrimStart(this);ImposeMaxLength(this, 100);" onblur="TrimInput(this);this.onchange();" onkeyup="this.onchange();"
        onkeypress="this.onchange();"></textarea>
      </p>
    </div>
    <div id="uxEditProjectButtons" class="popup-footer">
      <input type="button" class="" id="uxSubmitEditProject" onclick="SubmitEditProject();" title="Submit edits to project" value="Submit" />
      <input type="button" class="" id="uxEditProjectCancel" onclick="CloseForm('uxEditProject');" title="Cancel project edit and return to project list" value="Cancel" />
    </div>
  </div>
</div>

<div id="uxDuplicateProjectContainer" class="display-none popup-container" >
  <div id="uxDuplicateProjectBackground" class="popup-background"></div>
  <div id="uxDuplicateProjectForm" class="popup-form draggable">
    <div id="uxDuplicateProjectHeader" class="popup-header">
      <h3>Duplicate a project</h3>
      <div class="popup-control-panel">
        <img title="This form is draggable when you see the arrows" alt="Draggable form" src="/images/move.png" 
              class="control-drag" />
        <img title="Close this form" alt="Close form" src="/images/close.png"
              id="uxDuplicateProjectClose" class="control-close" onclick="CloseForm('uxDuplicateProject');" />
      </div>
      <hr />
      <p class="clear-align"><label>This tool will create a duplicate of project <label data-link="{:selected().Project.Name}"></label>. Please enter the following information. Some fields are required.</label></p>
    </div>
    <div id="uxDuplicateProjectMain">
      <p><label id="uxDuplicateProjectNameInfo" class="left-column">New Project Name (required):</label>
      <input data-link="{:selected().Project.Name} disabled{:selectedID === '0'}" id="uxDuplicateProjectName" type="text" name="text" maxlength="50" 
        onchange="TrimStart(this);ValidateProjectName(this);ImposeMaxLength(this, 50);" onblur="TrimInput(this);this.onchange();" onkeyup="this.onchange();"
        onkeypress="this.onchange();" class="right-column" />
      </p>
      <p>
        <span class="text-small left-column"><span id="uxDuplicateProjectNameCount" data-link="{:50-selected().Project.Name.length} disabled{:selectedID === '0'}">50</span><span> characters remaining</span></span>
        <label id="uxDuplicateProjectNameDesc" class="text-small right-column">Name must begin with a letter and may contain letters, numbers, dashes and underscores.</label>
      </p>
      <p class="display-none">
        <label id="uxDuplicateProjectStateInfo" class="left-column">State: </label>
        <label id="uxDuplicateProjectState" class="right-column"></label>
      </p>
      <p class="display-none">
        <label id="uxDuplicateProjectCountyInfo" class="left-column">County: </label>
        <label id="uxDuplicateProjectCounty" class="right-column"></label>
      </p>
      <p>
      <label id="uxDuplicateOperationNotesInfo" class="left-column">Notes:
        <br /><span class="text-small"><span id="uxDuplicateProjectNotesCount" data-link="{:100-selected().OperationDatum.Notes.length} disabled{:selectedID === '0'}">100</span><span> characters remaining</span></span></label>
      <textarea data-link="{:selected().OperationDatum.Notes} disabled{:selectedID === '0'}" id="uxDuplicateProjectNotes" 
        name="text" class="right-column" cols="34" rows="3" 
        onchange="TrimStart(this);ImposeMaxLength(this, 100);" onblur="TrimInput(this);this.onchange();" onkeyup="this.onchange();"
        onkeypress="this.onchange();"></textarea>
      </p>
    </div>
    <div id="uxDuplicateProjectButtons" class="popup-footer">
      <input type="button" class="" id="uxDuplicateProjectSubmit" onclick="return SubmitDuplicateProject('Duplicate');" title="Submit the project for creation" value="Submit" />
      <input type="button" class="" id="uxDuplicateProjectCancel" onclick="CloseForm('uxDuplicateProject');" title="Cancel project creation" value="Cancel" />
    </div>
  </div>
</div>

</script>

<script type="text/javascript">
  function ConvertIntMonth(intgr) {
    var retVal = intgr;
    try { retVal = monthsAbbr[intgr]; } catch (e) { /*no error*/ }
    return retVal;
  }
  $.views.tags("CalMonth", ConvertIntMonth);
  var projectsTmpl = $.templates("#projectsTmpl");
</script>
  
  </div>
 <%--</div>--%>
</div>

<%-- BEGIN: popup divs area --%>

<div id="uxTransferProjectContainer" class="display-none popup-container" >
  <div id="uxTransferProjectBackground" class="popup-background"></div>
  <div id="uxTransferProjectForm" class="popup-form draggable">
    <div id="uxTransferProjectHeader" class="popup-header">
      <h3>Transfer a project</h3>
      <div class="popup-control-panel">
        <img title="This form is draggable when you see the arrows" alt="Draggable form" src="/images/move.png" 
              class="control-drag" />
        <img title="Close this form" alt="Close form" src="/images/close.png"
              id="uxTransferProjectClose" class="control-close" onclick="CloseForm('uxTransferProject');" />
      </div>
      <hr />
      <p class="clear-align"><label>This tool allows you to transfer a duplicate of your project, <label id="uxTransferProjectName"></label>, to another user. An exact username is required. Your project will not be changed on your end.</label></p>
    </div>
    <div id="uxTransferProjectMain">
      <p><label id="uxTransferProjectUserNameInfo" class="left-column">Username (required):</label>
      <input id="uxTransferProjectUserName" type="text" name="text" maxlength="100" 
        onchange="ImposeMaxLength(this, 100);" onblur="this.onchange();" onkeyup="this.onchange();"
        onkeypress="this.onchange();" class="right-column" />
      </p>
      <p class="clear-align"><label>An email will be sent to the entered user (and copied to you). The text of that email is below, where "{user}" will be replaced with the recipient's name.</label></p>
      <p>
      <p class="clear-align"><label>You may add your own text to the email in the following box, which will be inserted where you see "{additional text}".</label></p>
      <p>
      <label id="uxTransferNotesInfo" class="left-column">Additional Text:
        <br /><span class="text-small"><span id="uxTransferNotesCount">100</span><span> characters remaining</span></span></label>
      <textarea id="uxTransferNotes" 
        name="text" class="right-column" cols="34" rows="3" 
        onchange="TrimStart(this);ImposeMaxLength(this, 100);" onblur="TrimInput(this);this.onchange();" onkeyup="this.onchange();"
        onkeypress="this.onchange();"></textarea>
      </p>
      <p class="clear-align display-none"><textarea class="full-width" id="uxTransferProjectEmail2" disabled rows="12" >
Dear {user},
A TerLoc project has been transferred to you from user {sender}. The project should show up on your Project Mgmt page at terrace.missouri.edu with the name {projectname}.
If you don’t see the project (you may need to refresh the page if you already have it open), please forward this email to Kevin Atherton at athertonk@missouri.edu for troubleshooting and keep the sender cc’ed above so they know something went wrong.
If you think you received this project in error, please contact the sender at the cc’ed email address above. You may delete the project using the TerLoc tools.

Thank you.

{additional text}
      </textarea></p>
      <p class="clear-align"><textarea class="full-width" id="uxTransferProjectEmail" disabled rows="11" >
Hello {user},
I've transferred a TerLoc project to you. 
The project should show up on your Project Mgmt page at terrace.missouri.edu with the name {projectname}.
If you don’t see the project (you may need to refresh the page if you already have it open), please reply back so I know something went wrong and add Kevin Atherton (athertonk@missouri.edu) to the recipients so he can troubleshoot. 
If I accidently sent you this project in error, please let me know.
Thank you.

{additional text}
      </textarea></p>
    </div>
    <div id="uxTransferProjectButtons" class="popup-footer">
      <input type="button" class="" id="uxTransferProjectSubmit" onclick="return SubmitTransferProject('Transfer');" title="Submit the project for creation" value="Submit" />
      <input type="button" class="" id="uxTransferProjectCancel" onclick="CloseForm('uxTransferProject');" title="Cancel project creation" value="Cancel" />
    </div>
  </div>
</div>

<div id="uxImportToolsContainer" class="display-none popup-container" >
  <div id="uxImportToolsBackground" class="popup-background"></div>
  <div id="uxImportToolsForm" class="popup-form draggable">
    <div id="uxImportToolsHeader" class="popup-header">
      <h3>Import Tools</h3>
      <div class="popup-control-panel">
        <img title="This form is draggable when you see the arrows" alt="Draggable form" src="/images/move.png" 
              class="control-drag" />
        <img title="Close this form" alt="Close form" src="/images/close.png"
              data-form-cancel="field-tools" class="control-close" onclick="CloseForm('uxImportTools');" />
      </div>
      <hr id="uxImportToolsDragLineUpper" />
    </div>
    <div id="uxImportToolsMain" >
      <p class=" center" id=""><label id="uxImportInfo">This tool allows you to import files into your project.</label></p>
      <div class=" display--none">
        <p class=" center">
          <label id="uxImportFileOptions" class="">Select the files you wish to import.</label>
        </p>
        <p class="display-none">
          <label class="left-column"><input type="checkbox" id="uxImportGisXml" value="gisxml" /></label>
          <label class="right-column">An XML file with well-known binary shapes for fields</label>
        </p>
        <p>
          <label class="left-column"><input type="checkbox" id="uxImportGis" value="gis" /></label>
          <label class="right-column">GIS Shapefile of fields</label>
        </p>
      </div>
      <br /><br />
      <p id="uxImportZipHolder" class="display-none">
        <label id="uxImportZipLinkHolderInfo" class=" left-column">Click to import:</label>
        <label id="uxImportZipLinkHolder" class="right-column"></label>
      </p>
      <br /><br />
    </div>
    <div id="uxImportToolsButtons" class="popup-footer">
      <input type="button" class="" id="uxImportCancel" onclick="CloseForm('uxImportTools');" title="Close this menu" value="Close" />
    </div>
  </div>
</div>

<div id="uxDownloadToolsContainer" class="display-none popup-container" >
  <div id="uxDownloadToolsBackground" class="popup-background"></div>
  <div id="uxDownloadToolsForm" class="popup-form draggable">
    <div id="uxDownloadToolsHeader" class="popup-header">
      <h3>Download Tools</h3>
      <div class="popup-control-panel">
        <img title="This form is draggable when you see the arrows" alt="Draggable form" src="/images/move.png" 
              class="control-drag" />
        <img title="Close this form" alt="Close form" src="/images/close.png"
              data-form-cancel="field-tools" class="control-close" onclick="CloseForm('uxDownloadTools');" />
      </div>
      <hr id="uxDownloadToolsDragLineUpper" />
    </div>
    <div id="uxDownloadToolsMain" >
      <p class=" center" id=""><label id="uxDownloadInfo">This tool will create a zip file containing the selected files below.</label></p>
      
      <div>
        <p class="left clear" id="P2">
          <label id="uxExportFileOptions" class="">Select the files you wish to download:</label>
        </p>
        <p class="clear"></p>
        <p class="display---none">
          <label class="left-column"><input type="checkbox" id="uxExportGisXml" value="gisxml" /></label>
          <label class="right-column">A LandXML file of all features</label>
        </p>
        <p>
          <label class="left-column"><input type="checkbox" id="uxExportGis" value="gis" /></label>
          <label class="right-column">GIS Shapefiles of fields</label>
        </p>
        <p>
          <label class="left-column"><input type="checkbox" id="uxExportClipper" value="clipper" /></label>
          <label class="right-column">Missouri Clipper files</label>
          &nbsp;<img id="uxClipperInfo" src="/images/about.gif" alt="Show clipper info."
            title="" onmouseover="$('[id$=uxHoverInfo]').removeClass('display-none');" onmouseout="$('[id$=uxHoverInfo]').addClass('display-none');" />
          <label id="uxHoverInfo" class="display-none"><br />
          This will send your features to the Missouri Clipper (clipper.missouri.edu) which will extract 
            imagery, soils and related files for your operation.
          This may take up to three minutes, so please be patient.
          </label>
        </p>
        <p class="warning center display-none" id="uxOneFileWarning">
          <label class="warning-text ">Please select at least one file option.</label>
        </p>
      </div>
      <br /><br />
      <p id="uxDownloadZipHolder" class="display-none">
        <label id="uxDownloadZipLinkHolderInfo" class=" left-column">Click to download:</label>
        <label id="uxDownloadZipLinkHolder" class="right-column"></label>
      </p>
      <br /><br />
    </div>
    <div id="uxDownloadToolsButtons" class="popup-footer">
      <input type="button" class="" id="uxDataDumper" onclick="SubmitDownload();"
            title="Create a downloadable zip file containing the selected file(s)." value="Create Zip" />
      <input type="button" class="" id="uxDownloadCancel" onclick="CloseForm('uxDownloadTools');" title="Close this menu" value="Close" />
    </div>
  </div>
</div>

<div id="uxCreateProjectContainer" class="display-none popup-container" >
  <div id="uxCreateProjectBackground" class="popup-background"></div>
  <div id="uxCreateProjectForm" class="popup-form draggable">
    <div id="uxCreateProjectHeader" class="popup-header">
      <h3>Create a new project</h3>
      <div class="popup-control-panel">
        <img title="This form is draggable when you see the arrows" alt="Draggable form" src="/images/move.png" 
              class="control-drag" />
        <img title="Close this form" alt="Close form" src="/images/close.png"
              id="uxCreateProjectClose" class="control-close" onclick="CloseForm('uxCreateProject');" />
      </div>
      <hr />
      <p><label id="uxCreateProjectInfo" class="">Please enter the following information. Some fields are required.</label></p>
    </div>
    <div id="uxCreateProjectMain">
      <p><label id="uxCreateProjectNameInfo" class="left-column">Project Name (required):</label>
      <input id="uxCreateProjectName" type="text" name="text" maxlength="50" 
        onchange="TrimStart(this);ValidateProjectName(this);ImposeMaxLength(this, 50);" onblur="TrimInput(this);this.onchange();" onkeyup="this.onchange();"
        onkeypress="this.onchange();" class="right-column" />
      </p>
      <p><span class="text-small left-column"><span id="uxCreateProjectNameCount">50</span><span> characters remaining</span></span>
        <label id="uxCreateProjectNameDesc" class="text-small right-column">Name must begin with a letter and may contain letters, numbers, dashes and underscores.</label>
      </p>
      <p>
        <label id="uxCreateProjectStateInfo" class="left-column">State (required):</label>
        <select id="uxCreateProjectState" onchange="FillCountys(this);" class="wd-med right-column">
          <option value="-1">-- SELECT --</option>
          <option value="Alabama">Alabama</option><option value="Alaska">Alaska</option><option value="Arizona">Arizona</option>
          <option value="Arkansas">Arkansas</option><option value="California">California</option><option value="Colorado">Colorado</option>
          <option value="Connecticut">Connecticut</option><option value="Delaware">Delaware</option><option value="District of Columbia">District of Columbia</option>
          <option value="Florida">Florida</option><option value="Georgia">Georgia</option><option value="Hawaii">Hawaii</option>
          <option value="Idaho">Idaho</option><option value="Illinois">Illinois</option><option value="Indiana">Indiana</option>
          <option value="Iowa">Iowa</option><option value="Kansas">Kansas</option><option value="Kentucky">Kentucky</option>
          <option value="Louisiana">Louisiana</option><option value="Maine">Maine</option><option value="Maryland">Maryland</option>
          <option value="Massachusetts">Massachusetts</option><option value="Michigan">Michigan</option><option value="Minnesota">Minnesota</option>
          <option value="Mississippi">Mississippi</option><option value="Missouri">Missouri</option><option value="Montana">Montana</option>
          <option value="Nebraska">Nebraska</option><option value="Nevada">Nevada</option><option value="New Hampshire">New Hampshire</option>
          <option value="New Jersey">New Jersey</option><option value="New Mexico">New Mexico</option><option value="New York">New York</option>
          <option value="North Carolina">North Carolina</option><option value="North Dakota">North Dakota</option><option value="Ohio">Ohio</option>
          <option value="Oklahoma">Oklahoma</option><option value="Oregon">Oregon</option><option value="Pennsylvania">Pennsylvania</option>
          <option value="Rhode Island">Rhode Island</option><option value="South Carolina">South Carolina</option><option value="South Dakota">South Dakota</option>
          <option value="Tennessee">Tennessee</option><option value="Texas">Texas</option><option value="Utah">Utah</option>
          <option value="Vermont">Vermont</option><option value="Virginia">Virginia</option><option value="Washington">Washington</option>
          <option value="West Virginia">West Virginia</option><option value="Wisconsin">Wisconsin</option><option value="Wyoming">Wyoming</option>
        </select>
      </p>
      <p>
        <label id="uxCreateProjectCountyInfo" class="left-column">County (required):</label>
        <select id="uxCreateProjectCounty" class="wd-med right-column">
          <option value="-1"> -- SELECT STATE FIRST -- </option>
        </select>
      </p>
      <p>
        <label id="uxCreateProjectStartCalYearInfo" class="left-column">Start Year:</label>
        <select id="uxCreateProjectStartCalYear" class="wd-med right-column">
          <option value="value">text</option>
        </select>
      </p>
      <p>
      <label id="uxCreateProjectStartCalMonthInfo" class="left-column">Start Month:</label>
        <select id="uxCreateProjectStartCalMonth" class="wd-med right-column">
          <option value="1">January</option>
          <option value="2">February</option>
          <option value="3">March</option>
          <option value="4">April</option>
          <option value="5">May</option>
          <option value="6">June</option>
          <option value="7">July</option>
          <option value="8">August</option>
          <option value="9">September</option>
          <option value="10">October</option>
          <option value="11">November</option>
          <option value="12">December</option>
        </select>
      </p>
      <p><label id="uxCreateProjectOperationNameInfo" class="left-column">Operation:</label>
      <input id="uxCreateProjectOperationName" type="text" maxlength="30" onblur="TrimInput(this);" class="right-column" />
      </p>
      <p><label id="uxCreateProjectAddressInfo" class="left-column">Address:</label>
      <input id="uxCreateProjectAddress" type="text" maxlength="30" onblur="TrimInput(this);" class="right-column" />
      </p>
      <p><label id="uxCreateProjectCityInfo" class="left-column">City:</label>
      <input id="uxCreateProjectCity" type="text" maxlength="20" onblur="TrimInput(this);" class="right-column" />
      </p>
      <p>
      <label id="uxCreateProjectZipInfo" class="left-column">Zip:</label>
      <input id="uxCreateProjectZip" type="text" maxlength="10" onblur="TrimInput(this);" class="right-column" />
      </p>
      <p>
      <label id="uxCreateProjectContactInfo" class="left-column">Contact:</label>
      <input id="uxCreateProjectContact" type="text" maxlength="30" onblur="TrimInput(this);" class="right-column" />
      </p>
      <p>
      <label id="uxCreateProjectContactOfficePhoneInfo" class="left-column">Office Phone:</label>
      <input id="uxCreateProjectContactOfficePhone" type="text" maxlength="14" onblur="TrimInput(this);" class="right-column" />
      </p>
      <p>
      <label id="uxCreateProjectContactHomePhoneInfo" class="left-column">Home Phone:</label>
      <input id="uxCreateProjectContactHomePhone" type="text" maxlength="14" onblur="TrimInput(this);" class="right-column" />
      </p>
      <p>
      <label id="uxCreateProjectContactEmailInfo" class="left-column">Email:</label>
      <input id="uxCreateProjectContactEmail" type="text" maxlength="40" onblur="TrimInput(this);" class="right-column" />
      </p>  
      <p>
      <label id="uxCreateProjectOperationNotesInfo" class="left-column">Notes:
          <br /><span class="text-small"><span id="uxCreateProjectNotesCount">100</span><span> characters remaining</span></span></label>
      <textarea id="uxCreateProjectNotes" class="right-column" cols="34" rows="3"
            onchange="TrimStart(this);ImposeMaxLength(this, 100);" onblur="TrimInput(this);this.onchange();" onkeyup="this.onchange();"
            onkeypress="this.onchange();"></textarea>
      </p>
    </div>
    <div id="uxCreateProjectButtons" class="popup-footer">
      <input type="button" class="" id="uxCreateProjectSubmit" onclick="return SubmitCreateProject();" title="Submit the project for creation" value="Submit" />
      <input type="button" class="" id="uxCreateProjectCancel" onclick="CloseForm('uxCreateProject');" title="Cancel project creation" value="Cancel" />
    </div>
  </div>
</div>

<%-- END: popup divs area --%>

<div id="whitespace">
&nbsp;<br />
&nbsp;<br />
&nbsp;<br />
&nbsp;<br />
</div>

</asp:Content>
﻿var obj = {};
//$(document).ready(function () {
var ctop = $(".menutop");
var cleft = $(".leftarea");
var cright = $(".rightarea .content");
var cmsg = $(".rightarea .message");
var getModels = function (path, f) {
    $.get("/admin/api/models?path=" + path, f);
};
var sortNodes = function (path, items, f) {
    var items_str = "";
    $(items).each(function (i) {
        items_str += "items=" + this + "&";
    });
    items_str=items_str.substring(0, items_str.length - 1);
    $.ajax({
        url: "/admin/api/sort?path=" + path,
        data: items_str,
        traditional: true,
        success: f,
        type: "POST",
        datatype: "json"
    });
}
var getMarkup = function (path, type, variant, f) {
    $.get("/admin/api/edit?variant=" + variant + "&type=" + type + "&path=" + path, f,"html");
}
var getCreateDialog = function (f, t) {
    $.get("/admin/api/createdialog" + (t === undefined ? "" : "?type=" + t), f, "html");
}
var getSettings = function (f) {
    $.get("/admin/settings/edit", f, "html");
}
var getVariants = function (f) {
    $.get("/admin/api/variants", f);
}
var setPublish = function (id, f) {
    $.get("/admin/api/publish?id=" + id, f);
}
var setUnPublish = function (id, f) {
    $.get("/admin/api/unpublish?id=" + id, f);
}
var setDelete = function (id, f) {
    $.get("/admin/api/delete?id=" + id, f);
}
var getFieldGroups = function (t,f) {
    $.get("/admin/api/fieldgroups?type="+t, f);
}
var getLocalisationDialog = function (p, f) {
    $.get("/admin/api/LocalisationDialog?path=" + p, f);
}
var getDomainMappingDialog = function (p, f) {
    $.get("/admin/api/DomainMappingDialog?path=" + p, f);
}
var getTasks = function (f) {
    $.get("/admin/task/index", f);
}
var getTaskCreateDialog = function (f) {
    $.get("/admin/task/CreateTaskDialog", f);
}
var getTaskMarkup = function (f,type,id) {
    var type = isNullEmpty(type) ? "" : ("type=" + type);
    var id = isNullEmpty(id) ? "" : "id=" + id;
    if (!isNullEmpty(type))
        type+="&"
    $.get("/admin/task/Edit?"+type+id, f);
}
var showTasks = function () {
    getTasks(function (data) {
        cright.html(data);
        cright.find("a").click(function (e) {
            e.preventDefault();
            var el = $(this);
            if (el.hasClass("create_task")) {
                createTask();
                return;
            }
            if (el.hasClass("delete"))
                if (!confirm("sure?"))
                    return;
            $.get(el.attr("href"), function (d) {
                cright.append(d);
            });
        });
    });
}
var createTask = function () {
    getTaskCreateDialog(function (data) {
        overlay(data, 400, 150);
        $(".overlayinner button").click(function (e) {
            e.preventDefault();
            var typeSelect = $(".overlayinner select[name=type]");
            var type = typeSelect.val();
            getTaskMarkup(function (data) {
                overlayClose();
                overlay(data, 500);
                var form = $(".overlayinner form");
                wireForm(form, function (data) {
                    msg(true, "task updated");
                    overlayClose();
                }, function (data) {
                    msg(false, data.message);
                });
            }, type);
        });
    });
}
var wireForm = function (form, success, fail) {
    $.validator.unobtrusive.parse(form);
    form.submit(function (e) {
        if (form.valid()) {
            e.preventDefault();
            var values = form.serialize();
            form.find("input[disabled]").each(function () {
                //var inp = $(this);
                //console.log("disabled input %o", inp);
                //values += "&" + inp.attr("name") + "=" + inp.val();
            });
            console.log("serialized data: %s", values);
            $.post(form.attr("action"), values, function (data) {
                console.log("data %s", data);
                if (data.success == true) {
                    success(data);
                } else {
                    fail(data);
                }
            });
        }
    });
}
var newContent = function (path, type) {
    getCreateDialog(function (data) {
        overlay(data, 400, 250, 100);
        $(".overlayinner button").click(function () {
            var type = $(".overlayinner select[name=type]").val();
            var variant = $(".overlayinner select[name=variant]").val();
            overlayClose()
            displayMarkup(path, type, variant);
        });
    }, type);
}
var publishedContent = [];
var haveChildren = [];
var getDrawContent = function (path, el, sortable) {
    if (el == undefined) {
        var nodeParent = dirOfPath(path);
        el = cleft.find(".node[data-children_path='" + nodeParent + "']");
    }
    getContent(path, function (data) {
        for (var k in data.published) {
            publishedContent[k] = data.published[k];
        }
        for (var i = 0; i < data.children.length; i++) {
            haveChildren[data.children[i]] = true;
        }
        draw(data.current, el, sortable);
        el.find(".node").each(function () {
            var n = $(this);
            if (!haveChildren[n.attr("data-path")])
                n.find(".expand").css({visibility:"hidden"});
        });
    });
}
var defaultLanguage = "en-gb";
var draw = function (data, el, sortable) {
    var str = "";
    var toAppend = $("<ul/>");
    for (var p in data) {//paths as keys
        var variants = [];
        for (var v in data[p]) {//variant as keys
            variants.push(v);
            var node;
            if (!!data[p][defaultLanguage])
                node = data[p][defaultLanguage];
            else
                node = data[p][v];
            var elnode = $("<li/>").addClass("node");
            if (!node.Published)
                elnode.addClass("unpublished");
            elnode.append($("<i class=\"icon-chevron-right expand\"></i>"))
            elnode.append($("<i class=\"icon-cog menu\"></i>"))
                    .append("<span class='nodename'>" + node.NodeName + "&nbsp;" + "</span>");
            for (var i = 0; i < variants.length; i++) {
                elnode.append(
                            $("<span class=\"variant\"/>").attr("data-variant", variants[i]).html(variants[i])
                        );
            }
            elnode.attr({
                "data-type_chain": typeFromChain(node.TypeChain)
                , "data-type": node.Type
                , "data-id": node.Id
                , "data-path": node.Path
                , "data-nodename": node.NodeName
                , "data-variants": variants.join(",")
                , "data-parent_path": dirOfPath(node.Path)
                , "data-children_path": node.Path + "/"
                , "data-published": node.Published
            });
            toAppend.append(elnode);
        }
    }
    el.find("ul").remove();
    el.append(toAppend);
    if (sortable) {
        toAppend.sortable({
            update: function (event, ui) {
                var parent = ui.item.parents("li[data-children_path]:first");
                var sortPath = parent.attr("data-children_path");
                var items = [];
                parent.find("li.node").each(function () {
                    items.push($(this).attr("data-path"));
                });
                sortNodes(sortPath, items, function () { });
            }
        });
    }
}

var displayMarkup = function (path, type, variant) {
    getMarkup(path, type, variant, function (data) {
        cright.html(data);
        //get field groups and build tabs
        getFieldGroups(type, function (data) {
            var groups = [];
            $(data).each(function (i) {
                var val = this
                if (!groups.contains(val.split(':')[1]))
                    groups.push(val.split(":")[1]);
            });
            var tabHtml = '<ul class="nav nav-tabs">';
            $(groups).each(function (i) {
                var val = this;
                tabHtml += '<li class="' + (i == 0 ? "active" : "") + '"><a class="fieldtabs" href="#fieldtabs' + i + '">' + val + '</a></li>';
            });
            tabHtml += '<li class=""><a class="fieldtabs" href="#fieldtabs' + groups.length + '">default</a></li>';
            tabHtml += '</ul>';

            tabHtml += '<div class="tab-content">';
            $(groups).each(function (i) {
                var val = this;
                tabHtml += '<div data-group="' + val + '" class="tab-pane ' + (i == 0 ? "active" : "") + '" id="fieldtabs' + i + '">&nbsp;</div>';
            });
            tabHtml += '<div data-group="default" class="tab-pane" id="fieldtabs' + groups.length + '">&nbsp;</div>';
            tabHtml += "</div>";
            cright.find("form").prepend(tabHtml);
            cright.find(".nav .fieldtabs").click(function (e) {
                e.preventDefault();
                $(this).tab("show");
            });
            $(data).each(function (i) {
                var val = this;
                var type = val.split(":")[0];
                var group = val.split(":")[1];
                var field = val.split(":")[2];
                var fieldWrapper = $(".fieldwrapper[data-fieldname='" + field + "']");
                var groupContainer = $(".tab-pane[data-group='" + group + "']");
                groupContainer.append(fieldWrapper);
            });
            cright.find("div.fields>.fieldwrapper").appendTo(cright.find("[data-group='default']"));

        });
        wireForm(cright.find('form'), function (data) {
            msg(true, "content updated");
            getDrawContent(path,undefined,true);
        }, function (data) {
            msg(false, data.message);
        });
        afterDom();
    });
}
var msg = function (success, str) {
    var btnClass = "";
    if (success === false) { btnClass = "btn-danger"; }
    else if (success === true) { btnClass = "btn-success"; }
    var el = $("<div style='display:none;' class='btn " + btnClass + "'>" + str + "</div>");
    var remove = $("<div class='btn btnclose'>x</div>").click(function () { $(this).parent().remove(); });
    el.append(remove);
    cmsg.append(el);
    el.fadeIn();
}

var getContent = function (path, f) {
    $.get("/admin/api/content?path=" + path, f);
};

var overlayClose = function () {
    $(".overlayinner,.overlay").remove();
}
var overlay = function (el, width, height, top) {
    var ov = $("<div class='overlay'/>");
    var inner = $("<div class='overlayinner container_12'/>");
    if (!!width)
        inner.css({ width: width + "px" });
    if (!!height)
        inner.css({ height: height + "px" });
    if (!!top)
        inner.css({ top: top + "px" });
    var close = $("<div class='btn btn-link'><i class='icon-remove-sign'/>&nbsp;close</div>");
    close.click(function () {
        ov.remove();
        inner.remove();
        close.remove();
    });
    inner.append(close).append(el);
    cright.append(ov).append(inner);
    height = height || $(window).height() * 0.8;
    if (height)
        inner.css({ height: height + "px" });
    inner.css({ left: ($(window).width() - width || 960) / 2 + "px" });
    if (!top)
        inner.css({ top: ($(window).height() - height) / 2 + "px" });
}
var afterDomActions = [];
var onAfterDom = function (f) {
    afterDomActions.push(f);
}
var afterDom = function () {
    while (afterDomActions.length) {
        afterDomActions.pop()();
    }
}

$('a.settings').click(function (e) {
    e.preventDefault();
    getSettings(function (data) {
        overlay(data, 960, 500);
        afterDom();
        //$(".overlayinner .trigger").click();
        //setup validation
        wireForm($('.overlayinner form'), function (data) {
            overlayClose();
        }, function (data) {
            console.log("settings response %o", data);
            msg(false, d.message);
        });
    });
});
var dirOfPath = function (s) {
    //if (s == "/")
        //return s;
    if (s[s.length - 1] == "/")
        return s;
    return s.substring(0, s.lastIndexOf("/") + 1);
}
var isRootItem = function (s) {
    var matches = s.match(/\//g);
    if (matches==null)
        throw "isRootItem - invalid input: " + s;
    return matches.length == 1;
}
var typeFromChain = function (s) {
    return s.split(" ")[0];
}
var untranslated = function (variants) {
    var untranslated = [];
    $(languages).each(function () {
        if (!variants.split(",").contains(this.Key))
            untranslated.push(this.Key);
    });
    return untranslated.length == 0 ? false : untranslated;
}
//bindings
//root new content button
$(".create_default").show().click(function () { newContent("/"); });
//task list
$(".menutop .tasks").click(function (e) { e.preventDefault(); showTasks(); });
//content tree expand
$("body").on("click", "ul.content li.node i.expand", function () {
    //get children content
    var node = $(this).parent();
    console.log(node);
    var descendants = node.find("ul");
    if (descendants.length > 0) {//show
        if (descendants.first().is(":hidden")) {
            node.find("i.expand").removeClass("icon-chevron-right").addClass("icon-chevron-down");
            descendants.show();
        } else {//hide
            node.find("i.expand").removeClass("icon-chevron-down").addClass("icon-chevron-right");
            descendants.hide();
        }
    } else {
        getDrawContent(node.attr("data-children_path"),node,true);
        node.find("i.expand").removeClass("icon-chevron-right").addClass("icon-chevron-down");
    }
});
//node settings dropdown
cleft.find("ul.content").on("click", "li.node i.menu", function (e) {
    //display dropdown
    var node = $(this).parent();
    var left = node.offset().left;
    var top = node.offset().top + 30;
    var dropdown = $(".node-dropdown");
    dropdown
        .addClass("open")
        .css({ top: top + "px", left: left + "px" })
        .attr("data-context", node.attr("data-id"));
    e.stopPropagation();
    $("html").on("click", "", function () {
        dropdown.removeClass("open");
        $("html").off();
    });
    //filter menu items according to context
    //filter translation item
    var totranslate = untranslated(node.attr("data-variants"));
    if (totranslate)
        dropdown.find("a[data-action='translate']").parents("li").show();
    else
        dropdown.find("a[data-action='translate']").parents("li").hide();
    //filter publish/unpublish
    if (publishedContent[node.attr("data-path")] !=undefined) {
        dropdown.find("a[data-action='publish']").parents("li").hide();
        dropdown.find("a[data-action='unpublish']").parents("li").show();
    } else {
        dropdown.find("a[data-action='publish']").parents("li").show();
        dropdown.find("a[data-action='unpublish']").parents("li").hide();
    }
    //filter localisation

    //filter domain
    if (isRootItem(node.attr("data-path"))) {
        dropdown.find("a[data-action='domain']").parents("li").show();
    } else {
        dropdown.find("a[data-action='domain']").parents("li").hide();
    }    
});
//menu items
$(".node-dropdown a").click(function () {
    var el = $(this);
    var action = el.attr("data-action");
    var context = el.parents(".node-dropdown").attr("data-context");
    var node = $(".node[data-id='" + context + "']");
    switch (action) {
        case "create":
            newContent(node.attr("data-children_path"), node.attr("data-type"));
            break;
        case "translate":
            getCreateDialog(function (data) {
                overlay(data, 400, 300);
                var type = $(".overlayinner select[name=type]");
                var variant = $(".overlayinner select[name=variant] option");
                $(".overlayinner .typecontainer").hide();
                type.val(node.attr("data-type"));
                var variants = node.attr("data-variants").split(",");
                variant.each(function () {
                    //check value doesn't exist in variant list
                    var option = $(this);
                    var contains = false;
                    $(variants).each(function () {
                        if (this == option.val())
                            contains = true;
                    });
                    if (contains)
                        option.remove();
                });
                $(".overlayinner button").click(function () {
                    displayMarkup(node.attr("data-path"), node.attr("data-type"), variant.val());
                });
            },node.attr("data-type"));
            break;
        case "delete":
            if (confirm("sure?")) {
                setDelete(node.attr("data-id"), function (data) {
                    if (data.success === true) {
                        node.remove();
                    } else {
                        msg(data.message);
                    }
                });
            }; break;
        case "publish": setPublish(node.attr("data-id"), function (data) {
            if (data.success === true)
                var todo = ""; //refresh node
            else
                msg(data.message);

        }); break;
        case "unpublish": setUnPublish(node.attr("data-id"), function (data) {
            if (data.success === true)
                var todo = "";
            else
                msg(data.message);
        }); break;
        case "localisation":
            setLocalisation(node.attr("data-path"));
            break;
        case "domain":
            setDomainMapping(node.attr("data-path"));
            break;
    }
});

var setLocalisation = function (p) {
    getLocalisationDialog(p, function (data) {
        overlay(data,400,250);
        var form = $('.overlayinner form');
        wireForm(form, function (data) {
            overlayClose();
        }, function (data) {
            msg(data.message);
        });
    });
}
var setDomainMapping = function (p) {
    getDomainMappingDialog(p, function (data) {
        overlay(data,400,250);
        var form = $('.overlayinner form');
        wireForm(form, function (data) {
            overlayClose();
        }, function (data) {
            msg(data.message);
        });
    });
}
cleft.find("ul.content").on("click", "li.node span.variant", function () {
    //get markup
    var node = $(this).parents(".node");
    console.log(node);
    displayMarkup(node.attr("data-path"),node.attr("data-type"),$(this).attr("data-variant"));
});

//ini
var languages;
getVariants(function (data) {
    languages = data;
});
getDrawContent("/",undefined,true);

//extensions
String.prototype.isEmpty = function () {
    return this.replace(/\s/g, "").length == 0;
}
var isNullEmpty = function (s) {
    if (s == null || s == undefined) return true;
    return s.replace(/\s/g, "").length == 0;
}
Array.prototype.contains=function(v){
    var contains=false;
    for(var i =0; i<this.length;i++)
        if(this[i]==v)
            contains=true;
    return contains;
}
//});   
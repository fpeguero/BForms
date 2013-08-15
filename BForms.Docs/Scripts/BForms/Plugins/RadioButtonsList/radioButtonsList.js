﻿(function (factory) {
    if(typeof define === "function" && define.amd) {
        define(['jquery'], factory);
    }else {
        factory(window.jQuery);
    }
})(function ($) {
    
    $.fn.extend({
        radioButtonsList: function () {
            return $(this).each(function () {
                if (!$(this).hasClass("radioButtonsList-done")) {
                    return new RadioButtonsList($(this));
                }
            });
        }
    });

    $.fn.extend({
        radioButtonsListUpdateSelf: function (value) {
            return $(this).each(function () {
                if ($(this).hasClass("radioButtonsList-done")) {
                    return new RadioButtonsListUpdateSelf($(this), value);
                }
            });
        }
    });

    $.fn.extend({
        resetRadioButtons: function () {
            var $elem = $(this);
            if ($elem.hasClass('radioButtonsList-done')) {
                return new RadioButtonsListUpdateSelf($elem, $elem.data('initialvalue'));
            }
        }
    });

    var RadioButtonsList = (function () {
        function RadioButtonsList(self) {
            //#region functions
            function BuildWrapper(self) {
                var wrapper = $("<div tabindex='0' style='display: none;' id='" +
                            self.attr("id") + "_checkBox' " +
                            "class='checkbox_replace'></div>");

                if (self.hasClass('form-control')) wrapper.addClass('form-control');

                self.children().each(function () {
                    var anchorText = $(this).find("label").text();
                    var childSelect = $(this).find("input[type='radio']");
                    wrapper.append("<a data-value='" + childSelect.val() + "' class='option'>" + anchorText + "</a>");
                });
                UpdateWrapper(self, wrapper);
                return wrapper;
            }
            function UpdateWrapper(self, wrapper) {
                self.children().each(function () {
                    var selfRadio = $(this).find("input[type='radio']");
                    var wrapperRadio = wrapper.find("a[data-value='" + selfRadio.val() + "']");
                    if (selfRadio.is(":checked")) {
                        wrapperRadio.addClass("selected");
                    }
                    else {
                        wrapperRadio.removeClass("selected");
                    }
                });
            }
            function ToggleValue(self) {
                var selectedRadio = self.find("input[type='radio']:checked");
                var nextOption = selectedRadio.parent().next(":first");
                if (nextOption.length == 0) {
                    nextOption = self.children(":first");
                }
                var radioFromNextOption = nextOption.find("input[type='radio']");
                self.radioButtonsListUpdateSelf(radioFromNextOption.val());
            }
            //#endregion

            //#region actions
            var wrapper = BuildWrapper(self);
            self.after(wrapper);
            self.hide();
            wrapper.show();
            self.addClass("radioButtonsList-done");

            if (self.data("initialvalue") == undefined) {
                self.data("initialvalue", self.find('input[type="radio"]:checked').val());
                self.data('value', self.find('input[type="radio"]:checked').val());
            }
            //#endregion

            //#region registering events
            self.on("change", { self: self, wrapper: wrapper }, function (e) {
                UpdateWrapper(e.data.self, e.data.wrapper);
            });

            wrapper.on("click", "a.option", { self: self }, function (e) {
                e.preventDefault();
                e.data.self.radioButtonsListUpdateSelf($(this).data("value"));
                $(this).focus();
            });

            wrapper.on("keypress", { self: self }, function (e) {
                if (e.keyCode == 13 || (e.keyCode == 0 || e.keyCode == 32)) {//enter or space(0 mozilla, 32 IE)
                    e.preventDefault();
                    ToggleValue(e.data.self);
                }
            });

            wrapper.parents(":last").on("click", "label[for='" + self.attr("id") + "']", { self: self, wrapper: wrapper }, function (e) {
                ToggleValue(e.data.self);
                e.data.wrapper.focus();
            });
            //#endregion
        }
        return RadioButtonsList;
    })();

    var RadioButtonsListUpdateSelf = (function () {
        function RadioButtonsListUpdateSelf(self, value) {
            self.find("input[type='radio']").each(function () {
                $(this).prop("checked", ($(this).val() == value ? true : false));
                if ($(this).val() == value)
                    self.data('value', value);
            });
            self.trigger("change");
        }
        return RadioButtonsListUpdateSelf;
    })();
})
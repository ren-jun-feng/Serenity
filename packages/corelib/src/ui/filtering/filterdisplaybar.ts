﻿import { Fluent, localText } from "@serenity-is/base";
import { Decorators } from "../../types/decorators";
import { FilterDialog } from "./filterdialog";
import { FilterWidgetBase } from "./filterwidgetbase";

@Decorators.registerClass('Serenity.FilterDisplayBar')
export class FilterDisplayBar<P = {}> extends FilterWidgetBase<P> {

    protected renderContents() {
        var openFilterDialog = (e: Event) => {
            e.preventDefault();
            var dialog = new FilterDialog({});
            dialog.get_filterPanel().set_store(this.get_store());
            dialog.dialogOpen(null);
        };

        return Fluent("div")
            .append(Fluent("a")
                .addClass("reset")
                .attr("title", localText('Controls.FilterPanel.ResetFilterHint'))
                .on("click", e => {
                    e.preventDefault();
                    this.get_store().get_items().length = 0;
                    this.get_store().raiseChanged();
                }))
            .append(Fluent("a")
                .addClass("edit")
                .text(localText('Controls.FilterPanel.EditFilter'))
                .on("click", openFilterDialog))
            .append(Fluent("div")
                .addClass("current")
                .append(Fluent("span")
                    .addClass("cap")
                    .text(localText('Controls.FilterPanel.EffectiveFilter')))
                .append(Fluent("a")
                    .addClass("txt")
                    .on("click", openFilterDialog))).getNode();
    }

    protected filterStoreChanged() {
        super.filterStoreChanged();

        var displayText = this.get_store().get_displayText()?.trim() || null;

        Fluent(this.domNode).findFirst('.current').toggle(displayText != null);
        Fluent(this.domNode).findFirst('.reset').toggle(displayText != null);

        if (displayText == null)
            displayText = localText('Controls.FilterPanel.EffectiveEmpty');

        Fluent(this.domNode).findFirst('.txt').text('[' + displayText + ']');
    }
}

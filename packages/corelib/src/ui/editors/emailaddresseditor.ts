﻿import { Fluent } from "@serenity-is/base";
import { Decorators } from "../../decorators";
import { EditorProps } from "../widgets/widget";
import { StringEditor } from "./stringeditor";

@Decorators.registerEditor('Serenity.EmailAddressEditor')
export class EmailAddressEditor<P = {}> extends StringEditor<P> {

    static override createDefaultElement() { return Fluent("input").attr("type", "email").getNode(); }   

    constructor(props: EditorProps<P>) {
        super(props);
        this.domNode?.classList.add('email');
    }
}
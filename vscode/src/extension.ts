import * as vscode from 'vscode';
import * as path from 'path';
import { LanguageClient, TransportKind } from 'vscode-languageclient/node';
import { Trace } from 'vscode-jsonrpc';
import { acquireDotNet } from './acquireDotNet';

export async function activate(context: vscode.ExtensionContext) {
    const output = vscode.window.createOutputChannel('Yabal');

    let command, args;

    if (process.env.NODE_ENV === 'dev') {
        command = process.env.SERVER_PATH;
        args = [];
    } else {
        command = await acquireDotNet('8.0', 'GerardSmit.vscode-yabal');
        args = [path.join(__dirname, 'bin', 'Yabal.LanguageServer.dll')];
    }

    const transport = TransportKind.pipe
    const document = { scheme: 'file', language: 'html' };

    const client = new LanguageClient(
        'yabalLanguageServer', 
        'yabal',
        {
            run : { command, transport, args },
            debug: { command, transport, args }
        },
        {
            documentSelector: [ document ],
            synchronize: {
                configurationSection: 'yabalLanguageServer',
                fileEvents: vscode.workspace.createFileSystemWatcher('**/*.yabal')
            }
        },
        false
    );

    client.registerProposedFeatures();
    
    context.subscriptions.push(client.onNotification('yabal/log', function(data) {
        output.appendLine(data);
    }))

    await client.start();
}
import * as vscode from 'vscode';

export function activate(context: vscode.ExtensionContext) {
	console.log('Congratulations, your extension "fsharp-beautify" is now active!');
	
	const disposable1 = vscode.commands.registerCommand('fsharp-beautify.test1', () => {
		vscode.window.showInformationMessage('Test 1');
	});
	
	context.subscriptions.push(disposable1);
	
	const disposable2 = vscode.commands.registerCommand('fsharp-beautify.test2', () => {
		vscode.window.showInformationMessage('Test 2');
	});
	
	context.subscriptions.push(disposable2);
	
	const disposable3 = vscode.commands.registerCommand('fsharp-beautify.test3', () => {
		vscode.window.showInformationMessage('Test 3');
	});

	context.subscriptions.push(disposable3);
	
	const disposable4 = vscode.commands.registerCommand('fsharp-beautify.test4', () => {
		vscode.window.showInformationMessage('Test 4');
	});
	
	context.subscriptions.push(disposable4);
	
	const disposable5 = vscode.commands.registerCommand('fsharp-beautify.test5', () => {
		vscode.window.showInformationMessage('Test 5');
	});
	
	context.subscriptions.push(disposable5);
}


// This method is called when your extension is deactivated
export function deactivate() {}

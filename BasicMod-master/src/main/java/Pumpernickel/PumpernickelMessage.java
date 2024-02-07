package Pumpernickel;

import java.io.DataOutputStream;
import java.net.Socket;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.List;

public class PumpernickelMessage {
	public List<String> Lines = new ArrayList<String>();
	public void AddLine(String line) {
		Lines.add(line);
	}
	public void AddLine(int line) {
		Lines.add(line + "");
	}
	public void Send() {
        try {
            Socket clientSocket = new Socket("localhost",13076);
            DataOutputStream outToServer =
                    new DataOutputStream(clientSocket.getOutputStream());
            for (String line : Lines) {
            	outToServer.write((line + "\n").getBytes(StandardCharsets.UTF_8));
            }
            outToServer.write("Done\n".getBytes(StandardCharsets.UTF_8));
            outToServer.flush();
            clientSocket.close();
	    }
	    catch (Exception e) {
	    }
	}
}

// TCPServer project main.go
package main

import (
	"fmt"
	"net"
	"os"
//	"strings"
)

var ConnMap map[string]*net.Conn

func main() {
	ConnMap = make(map[string] *net.Conn)
	service := ":5000"
	tcpAddr, err := net.ResolveTCPAddr("tcp4", service)
	checkErr(err)
	listener, err := net.ListenTCP("tcp", tcpAddr)
	checkErr(err)
	for {
		conn, err := listener.Accept()
		if conn==nil{
			continue
		}
		addr:=conn.RemoteAddr().String()
		if addr=="" {
			continue
		}
		ConnMap[conn.RemoteAddr().String()] = &conn
		fmt.Println("Receive connect request from ",conn.RemoteAddr().String())
		if err != nil {
			continue
		}
		go handleClient(conn)
	}
}
func boradcastMessage(message string) {
	b := []byte(message)
	for _, conn := range ConnMap {
		(*conn).Write(b)
	}
}
func checkErr(err error) {
	if err != nil {
		fmt.Fprintf(os.Stderr, "Fatal error: %s", err.Error())
		os.Exit(1)
	}
}

func handleClient(conn net.Conn) {
	defer conn.Close()
	var buf [512]byte
	go func() {
		for {
			n, err := conn.Read(buf[0:])
			if err != nil {
				return
			}
			rAddr := conn.RemoteAddr()
content:=string(buf[0:n])
			if  len(content)>6 && content[0:6]=="voice:"{
				fmt.Println("voice message ", rAddr.String(),content )
				boradcastMessage(content)
			}else{
			fmt.Println("text message ", rAddr.String(),content )
			boradcastMessage(rAddr.String() + ":" + string(buf[0:n]))
			}
		}
	}()
	for {
		var input string
		fmt.Scanln(&input)
		if input != "" {
			conn.Write([]byte(input))
		}
	}
}

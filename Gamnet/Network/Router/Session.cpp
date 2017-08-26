#include "Session.h"
#include "../../Log/Log.h"
#include "../Tcp/Tcp.h"
#include "LinkManager.h"
#include "RouterCaster.h"


namespace Gamnet { namespace Network { namespace Router {

static boost::asio::io_service& io_service_ = Singleton<boost::asio::io_service>::GetInstance();

Session::Session() : Network::Tcp::Session() {
	onRouterConnect = [](const Address&) {};
	onRouterClose = [](const Address&) {};
}

Session::~Session() {}

void Session::OnAccept() {
	onRouterClose = LinkManager::onRouterClose;
}

void Session::OnConnect()
{
	watingSessionManager_.Clear();
	LOG(GAMNET_INF, "[Router] connect success..(remote ip:", remote_address->to_string(), ")");
	MsgRouter_SetAddress_Req req;
	req.tLocalAddr = Singleton<LinkManager>::GetInstance().localAddr_;
	if(false == Network::Tcp::SendMsg(std::static_pointer_cast<Session>(shared_from_this()), req))
	{
		return;
	}
	LOG(GAMNET_INF, "[Router] send SetAddress_Req (localhost->", remote_address->to_string(), ", service_name:", req.tLocalAddr.service_name.c_str(), ")");
}

void Session::OnClose(int reason)
{
	LOG(GAMNET_INF, "[Router] remote server closed(session_key:", session_key, ", ip:", remote_address->to_string(), ", service_name:", addr.service_name, ")");
	if("" != addr.service_name)
	{
		{
			std::lock_guard<std::mutex> lo(LinkManager::lock);
			onRouterClose(addr);
		}
		Singleton<RouterCaster>::GetInstance().UnregisterAddress(addr);
	}
	watingSessionManager_.Clear();
}

/*
int	Session::Send(const std::shared_ptr<Network::Tcp::Packet>& packet)
{
	msg_seq++;
	AsyncSend(packet);
	return packet->Size();
}

int Session::Send(const char* buf, int len)
{
	msg_seq++;
	AsyncSend(buf, len);
	return len;
}
*/
}}} /* namespace Gamnet */